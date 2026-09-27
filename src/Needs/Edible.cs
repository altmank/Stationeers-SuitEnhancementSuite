using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;

namespace SuitEnhancementSuite;

/// <summary>One kind of item the Food slot holds, and how one automatic bite of it is eaten.</summary>
internal abstract class Edible
{
    public abstract bool Accepts(Thing thing);

    /// <summary>Eats one bite of <paramref name="thing"/>, which <see cref="Accepts"/> has accepted.</summary>
    public abstract bool TryEat(Thing thing, Human human, ConsumptionPolicy policy, out float amount);

    /// <summary>How <paramref name="thing"/>, which <see cref="Accepts"/> has accepted, ranks as a Food slot refill.</summary>
    public abstract FoodOffer Offer(Thing thing, Human human);

    protected static NeedLevel NutritionOf(Human human) => new(human.Nutrition, human.GetNutritionStorage());

    protected static FoodGrade GradeOf(INutrition food) => food.GetFoodQuality() switch
    {
        FoodQuality.Raw => FoodGrade.Raw,
        FoodQuality.Cooked => FoodGrade.Cooked,
        FoodQuality.Canned => FoodGrade.Canned,
        FoodQuality.Complex => FoodGrade.Complex,
        _ => FoodGrade.None,
    };
}

/// <summary>
/// A fractional food (cereal bar, canned food, pie, milk bottle): the bite is just the portion that fills the
/// stomach, and the item keeps the rest.
/// </summary>
internal sealed class PortionedFood : Edible
{
    public override bool Accepts(Thing thing) => thing is Food food && food.NutritionValue > 0f;

    public override bool TryEat(Thing thing, Human human, ConsumptionPolicy policy, out float amount)
    {
        amount = 0f;
        var food = (Food)thing;
        if (food.Quantity <= ConsumptionPolicy.MinServing || !policy.ShouldEatPortion(NutritionOf(human))) return false;
        amount = food.EatAmount(human);
        if (amount <= ConsumptionPolicy.MinServing || food.Nutrition(amount) <= 0f) return false;
        human.OnConsumeFood(amount, food);
        human.OnFoodEaten(food);
        food.Quantity -= amount;
        return true;
    }

    public override FoodOffer Offer(Thing thing, Human human)
    {
        var food = (Food)thing;
        return FoodOffer.Portioned(GradeOf(food), food.GetNutritionalValue());
    }
}

/// <summary>
/// A food eaten in whole units (raw crops, cooked vegetables): one unit is eaten the way a hand-eaten one is, through
/// the item's own OnUseItem, which removes the unit and adds its nutrition up to the stomach's capacity.
/// Seeds are crops too but are never food here.
/// </summary>
internal sealed class WholeUnitFood : Edible
{
    public override bool Accepts(Thing thing) =>
        thing is Stackable and INutrition food and not Seed && food.GetNutritionalValue() > 0f;

    public override bool TryEat(Thing thing, Human human, ConsumptionPolicy policy, out float amount)
    {
        amount = 0f;
        var stack = (Stackable)thing;
        var food = (INutrition)thing;
        var unit = food.EatAmount(human);
        if (unit < 1f || stack.Quantity < (int)unit) return false;
        if (!policy.ShouldEatWholeUnit(NutritionOf(human), food.Nutrition(unit))) return false;
        var before = stack.Quantity;
        stack.OnUseItem(unit, human);
        // Stackable.OnUseItem does nothing for an item that is not instantiated.
        if (stack.Quantity >= before) return false;
        human.OnFoodEaten(food);
        amount = unit;
        return true;
    }

    public override FoodOffer Offer(Thing thing, Human human)
    {
        var food = (INutrition)thing;
        return FoodOffer.WholeUnit(GradeOf(food), food.Nutrition(food.EatAmount(human)));
    }
}
