namespace SuitEnhancementSuite;

/// <summary>
/// When an automatic bite or drink is due. A margin is the share of capacity that must be empty before the slot is
/// used. A whole-unit food is eaten only when all of its nutrition fits, unless nutrition is below the hunger
/// fallback (a zero fallback never wastes).
/// </summary>
internal sealed class ConsumptionPolicy(Percent foodMargin, Percent waterMargin, Percent hungerFallback)
{
    /// <summary>HydrationBase.OnUseSecondary refuses a drink when less than this much hydration is missing.</summary>
    public const float MinDrinkRoom = 0.005f;

    /// <summary>Smallest nutrition room, and smallest item quantity, worth a bite.</summary>
    public const float MinServing = 0.0001f;

    public Percent FoodMargin => foodMargin;

    public Percent WaterMargin => waterMargin;

    public Percent HungerFallback => hungerFallback;

    public bool ShouldDrink(NeedLevel hydration) =>
        hydration.Room > MinDrinkRoom && HasMargin(hydration, waterMargin);

    public bool ShouldEatPortion(NeedLevel nutrition) =>
        nutrition.Room > MinServing && HasMargin(nutrition, foodMargin);

    public bool ShouldEatWholeUnit(NeedLevel nutrition, float bite) =>
        bite > 0f && ShouldEatPortion(nutrition) && (bite <= nutrition.Room || IsStarving(nutrition));

    private bool IsStarving(NeedLevel nutrition) => nutrition.IsBelow(hungerFallback);

    private static bool HasMargin(NeedLevel need, Percent margin) => need.Room >= margin.Of(need.Capacity);
}
