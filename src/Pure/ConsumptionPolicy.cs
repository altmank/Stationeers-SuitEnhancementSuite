using System;

namespace SuitEnhancementSuite;

/// <summary>
/// When an automatic bite, drink or waste bag use is due. A margin is the share of capacity that must be empty before
/// the slot is used. A whole-unit food is eaten only when all of its nutrition fits, unless nutrition is below the
/// hunger fallback (a zero fallback never wastes). A waste bag is used once the waste need passes the waste threshold,
/// and never at or below the level from which the game allows it by hand.
/// </summary>
internal sealed class ConsumptionPolicy(Percent foodMargin, Percent waterMargin, Percent hungerFallback, Percent wasteThreshold)
{
    /// <summary>HydrationBase.OnUseSecondary refuses a drink when less than this much hydration is missing.</summary>
    public const float MinDrinkRoom = 0.005f;

    /// <summary>Smallest nutrition room, and smallest item quantity, worth a bite.</summary>
    public const float MinServing = 0.0001f;

    /// <summary>SanitationPacket.HumanChecks refuses a waste bag at or below this waste ratio.</summary>
    public const float MinWasteRatio = 0.25f;

    public Percent FoodMargin => foodMargin;

    public Percent WaterMargin => waterMargin;

    public Percent HungerFallback => hungerFallback;

    public Percent WasteThreshold => wasteThreshold;

    public bool ShouldDrink(NeedLevel hydration) =>
        hydration.Room > MinDrinkRoom && HasMargin(hydration, waterMargin);

    public bool ShouldEatPortion(NeedLevel nutrition) =>
        nutrition.Room > MinServing && HasMargin(nutrition, foodMargin);

    public bool ShouldEatWholeUnit(NeedLevel nutrition, float bite) =>
        bite > 0f && ShouldEatPortion(nutrition) && (bite <= nutrition.Room || IsStarving(nutrition));

    /// <param name="wasteRatio">The waste need, 0 (none) to 1 (the player soils the suit).</param>
    public bool ShouldRelieve(float wasteRatio) => wasteRatio > Math.Max(wasteThreshold.Of(1d), MinWasteRatio);

    private bool IsStarving(NeedLevel nutrition) => nutrition.IsBelow(hungerFallback);

    private static bool HasMargin(NeedLevel need, Percent margin) => need.Room >= margin.Of(need.Capacity);
}
