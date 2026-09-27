using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;

namespace SuitEnhancementSuite;

/// <summary>
/// A suit slot that serves one body need. What the slot accepts is exactly what it can serve, so the inventory
/// filter and the automatic eater cannot disagree.
/// </summary>
internal abstract class NeedSlot
{
    public static readonly NeedSlot Water = new WaterSlot();
    public static readonly NeedSlot Food = new FoodSlot();

    public abstract string Key { get; }

    /// <summary>Name of the need in log lines.</summary>
    public abstract string NeedName { get; }

    public static bool TryGet(string key, out NeedSlot slot)
    {
        slot = key == SlotKeys.Water ? Water : key == SlotKeys.Food ? Food : null;
        return slot != null;
    }

    public abstract bool Accepts(Thing thing);

    public abstract float Level(Human human);

    /// <summary>Serves the need from <paramref name="item"/> when the policy says it is due. Host only.</summary>
    /// <param name="amount">Item quantity used: litres of water, food units or portion of a food.</param>
    public abstract bool TryServe(DynamicThing item, Human human, ConsumptionPolicy policy, out float amount);

    private sealed class WaterSlot : NeedSlot
    {
        public override string Key => SlotKeys.Water;

        public override string NeedName => "hydration";

        public override bool Accepts(Thing thing) => thing is HydrationBase;

        public override float Level(Human human) => human.Hydration;

        public override bool TryServe(DynamicThing item, Human human, ConsumptionPolicy policy, out float amount)
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

        public override string NeedName => "nutrition";

        public override bool Accepts(Thing thing) => TryFindEdible(thing, out _);

        public override float Level(Human human) => human.Nutrition;

        public override bool TryServe(DynamicThing item, Human human, ConsumptionPolicy policy, out float amount)
        {
            amount = 0f;
            return TryFindEdible(item, out var edible) && edible.TryEat(item, human, policy, out amount);
        }

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
}
