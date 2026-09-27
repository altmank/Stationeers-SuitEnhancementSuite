namespace SuitEnhancementSuite;

/// <summary>
/// A food, ranked by: the higher quality tier first, which keeps the player's food quality (and so hydration capacity)
/// up; then, within a tier, portioned food before whole units, because a portion tops off exactly; then portioned food
/// with the most nutrition left (fewest refills), or the whole unit with the least nutrition per unit (it fits into a
/// topped-off stomach soonest).
/// </summary>
internal readonly struct FoodOffer : IOffer<FoodOffer>
{
    private readonly FoodGrade _grade;
    private readonly Serving _serving;
    private readonly float _nutrition;

    private FoodOffer(FoodGrade grade, Serving serving, float nutrition)
    {
        _grade = grade;
        _serving = serving;
        _nutrition = nutrition;
    }

    private enum Serving
    {
        WholeUnit,
        Portioned,
    }

    /// <param name="nutritionLeft">Nutrition of everything the item still holds.</param>
    public static FoodOffer Portioned(FoodGrade grade, float nutritionLeft) => new(grade, Serving.Portioned, nutritionLeft);

    /// <param name="unitNutrition">Nutrition of the one unit a bite eats.</param>
    public static FoodOffer WholeUnit(FoodGrade grade, float unitNutrition) => new(grade, Serving.WholeUnit, unitNutrition);

    public bool Beats(FoodOffer incumbent)
    {
        if (_grade != incumbent._grade) return _grade > incumbent._grade;
        if (_serving != incumbent._serving) return _serving == Serving.Portioned;
        return _serving == Serving.Portioned ? _nutrition > incumbent._nutrition : _nutrition < incumbent._nutrition;
    }
}
