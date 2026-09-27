namespace SuitEnhancementSuite;

/// <summary>
/// Food quality tiers, lowest first, as the game ranks them. Eating pulls the player's food quality toward the tier of
/// what was eaten, and low food quality shrinks hydration capacity.
/// </summary>
internal enum FoodGrade
{
    None,
    Raw,
    Cooked,
    Canned,
    Complex,
}
