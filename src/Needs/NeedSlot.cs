using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;

namespace SuitEnhancementSuite;

/// <summary>
/// A suit slot that serves one body need. What the slot accepts is exactly what it can serve, so the inventory
/// filter, the automatic refill and the automatic use cannot disagree.
/// </summary>
internal abstract class NeedSlot
{
    public static readonly NeedSlot Water = new WaterSlot();
    public static readonly NeedSlot Food = new FoodSlot();
    public static readonly NeedSlot Waste = new WasteSlot();

    public abstract string Key { get; }

    /// <summary>Name of the slot in log lines.</summary>
    public abstract string SlotName { get; }

    /// <summary>Name of the need in log lines.</summary>
    public abstract string NeedName { get; }

    public static bool TryGet(string key, out NeedSlot slot)
    {
        slot = key switch
        {
            SlotKeys.Water => Water,
            SlotKeys.Food => Food,
            SlotKeys.Waste => Waste,
            _ => null,
        };
        return slot != null;
    }

    public abstract bool Accepts(Thing thing);

    /// <summary>Accepted and not used up: something the slot can still serve from.</summary>
    public bool IsUsable(DynamicThing thing) => Accepts(thing) && HasContent(thing);

    public SlotContent Classify(DynamicThing occupant) =>
        occupant is null ? SlotContent.Empty : IsUsable(occupant) ? SlotContent.Usable : SlotContent.Spent;

    /// <summary>How <paramref name="replacement"/>, a usable item, enters the slot.</summary>
    public virtual ReplacementShape ShapeOf(DynamicThing replacement) => ReplacementShape.Whole;

    /// <summary>A fresh pick that ranks this slot's replacements. Its owner reuses it across passes.</summary>
    public abstract ReplacementPick CreatePick();

    public abstract float Level(Human human);

    /// <summary>Serves the need from the item in <paramref name="slot"/> when the policy says it is due. Host only.</summary>
    /// <param name="amount">Item quantity used: litres of water, food units or portion of a food, share of a waste bag.</param>
    public abstract StepResult Serve(Slot slot, PlayerInventory player, ConsumptionPolicy policy, out float amount);

    /// <summary>Whether an accepted item still holds something to serve.</summary>
    protected virtual bool HasContent(DynamicThing thing) => true;

    private sealed class WaterSlot : NeedSlot
    {
        public override string Key => SlotKeys.Water;

        public override string SlotName => "Water";

        public override string NeedName => "hydration";

        public override bool Accepts(Thing thing) => thing is HydrationBase;

        public override ReplacementPick CreatePick() =>
            new ReplacementPick<WaterOffer>(static (thing, _) => new WaterOffer(((HydrationBase)thing).Quantity));

        public override float Level(Human human) => human.Hydration;

        public override StepResult Serve(Slot slot, PlayerInventory player, ConsumptionPolicy policy, out float amount) =>
            StepResult.From(TryDrink(slot.Get(), player.Human, policy, out amount));

        protected override bool HasContent(DynamicThing thing) => ((HydrationBase)thing).Quantity > ConsumptionPolicy.MinServing;

        private static bool TryDrink(DynamicThing item, Human human, ConsumptionPolicy policy, out float amount)
        {
            amount = 0f;
            if (item is not HydrationBase drink || drink.Quantity <= 0f) return false;
            if (!policy.ShouldDrink(new NeedLevel(human.Hydration, human.GetHydrationStorage()))) return false;
            amount = drink.HydrateAmount(human);
            if (amount <= ConsumptionPolicy.MinServing) return false;
            drink.OnUseItem(amount, human);
            return true;
        }
    }

    private sealed class FoodSlot : NeedSlot
    {
        private static readonly Edible[] Edibles = [new PortionedFood(), new WholeUnitFood()];

        public override string Key => SlotKeys.Food;

        public override string SlotName => "Food";

        public override string NeedName => "nutrition";

        public override bool Accepts(Thing thing) => TryFindEdible(thing, out _);

        // A pick only weighs usable items, which are edible; the fallback offer ranks below every real food.
        public override ReplacementPick CreatePick() => new ReplacementPick<FoodOffer>(static (thing, human) =>
            TryFindEdible(thing, out var edible) ? edible.Offer(thing, human) : FoodOffer.WholeUnit(FoodGrade.None, float.MaxValue));

        public override float Level(Human human) => human.Nutrition;

        public override StepResult Serve(Slot slot, PlayerInventory player, ConsumptionPolicy policy, out float amount)
        {
            amount = 0f;
            var item = slot.Get();
            return StepResult.From(TryFindEdible(item, out var edible) && edible.TryEat(item, player.Human, policy, out amount));
        }

        protected override bool HasContent(DynamicThing thing) =>
            thing is INutrition food && food.GetNutritionalValue() > ConsumptionPolicy.MinServing;

        private static bool TryFindEdible(Thing thing, out Edible edible)
        {
            foreach (var candidate in Edibles)
            {
                if (!candidate.Accepts(thing)) continue;
                edible = candidate;
                return true;
            }
            edible = null;
            return false;
        }
    }

    /// <summary>
    /// Folded waste bags, and open bags that are not full. The open bag stays in the slot and is refilled until full,
    /// so one bag serves several uses before the next is unfolded.
    /// </summary>
    private sealed class WasteSlot : NeedSlot
    {
        public override string Key => SlotKeys.Waste;

        public override string SlotName => "Waste";

        public override string NeedName => "waste";

        public override bool Accepts(Thing thing) => thing is SanitationStack or SanitationPacket { IsStackFull: false };

        public override ReplacementShape ShapeOf(DynamicThing replacement) =>
            replacement is SanitationStack { Quantity: > 1 } ? ReplacementShape.OneOfStack : ReplacementShape.Whole;

        public override ReplacementPick CreatePick() => new ReplacementPick<WasteOffer>(static (thing, _) =>
            thing is SanitationPacket packet ? WasteOffer.Open(packet.Quantity / packet.MaxQuantity) : WasteOffer.Folded);

        public override float Level(Human human) => human.SanitationRatio;

        public override StepResult Serve(Slot slot, PlayerInventory player, ConsumptionPolicy policy, out float amount) =>
            WasteBags.Use(slot, player, policy, out amount);
    }
}
