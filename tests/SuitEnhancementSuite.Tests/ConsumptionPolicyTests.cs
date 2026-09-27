using Xunit;

namespace SuitEnhancementSuite.Tests;

public class ConsumptionPolicyTests
{
    private const float Stomach = 50f;
    private const float Pumpkin = 50f;
    private const float Tomato = 15f;

    private static ConsumptionPolicy Policy(double foodMargin = 1, double waterMargin = 1, double hungerFallback = 25) =>
        new(Percent.From(foodMargin), Percent.From(waterMargin), Percent.From(hungerFallback));

    private static NeedLevel Nutrition(float current) => new(current, Stomach);

    [Theory]
    [InlineData(40f, 25d, false)] // fits nowhere, not hungry enough
    [InlineData(10f, 25d, true)] // below the hunger fallback: eaten, 10 wasted
    [InlineData(10f, 0d, false)] // fallback 0 never wastes
    [InlineData(0f, 25d, true)] // empty stomach: all of it fits
    [InlineData(0f, 0d, true)]
    [InlineData(12.4f, 25d, true)]
    [InlineData(12.6f, 25d, false)]
    public void Pumpkin_is_eaten_only_when_it_fits_or_below_the_hunger_fallback(float nutrition, double fallback, bool eaten) =>
        Assert.Equal(eaten, Policy(hungerFallback: fallback).ShouldEatWholeUnit(Nutrition(nutrition), Pumpkin));

    [Theory]
    [InlineData(30f, true)]
    [InlineData(35f, true)] // room 15 = bite 15
    [InlineData(40f, false)]
    public void Tomato_is_eaten_when_its_nutrition_fits(float nutrition, bool eaten) =>
        Assert.Equal(eaten, Policy().ShouldEatWholeUnit(Nutrition(nutrition), Tomato));

    [Fact]
    public void A_whole_unit_without_nutrition_is_never_eaten() =>
        Assert.False(Policy().ShouldEatWholeUnit(Nutrition(0f), 0f));

    [Fact]
    public void A_full_stomach_eats_nothing_even_when_starving_is_configured_high()
    {
        Assert.False(Policy(foodMargin: 0, hungerFallback: 50).ShouldEatPortion(Nutrition(Stomach)));
        Assert.False(Policy(foodMargin: 0, hungerFallback: 50).ShouldEatWholeUnit(Nutrition(Stomach), Tomato));
    }

    [Theory]
    [InlineData(49.6f, false)] // room 0.4 < 1 % of 50
    [InlineData(49.4f, true)] // room 0.6
    public void Food_margin_1_keeps_the_player_topped_off(float nutrition, bool eaten) =>
        Assert.Equal(eaten, Policy(foodMargin: 1).ShouldEatPortion(Nutrition(nutrition)));

    [Theory]
    [InlineData(12.4f, true)] // below 25 %
    [InlineData(12.6f, false)]
    [InlineData(20f, false)]
    [InlineData(0f, true)]
    public void Food_margin_75_eats_only_below_a_quarter(float nutrition, bool eaten) =>
        Assert.Equal(eaten, Policy(foodMargin: 75).ShouldEatPortion(Nutrition(nutrition)));

    [Fact]
    public void The_food_margin_also_gates_whole_units() =>
        Assert.False(Policy(foodMargin: 75, hungerFallback: 50).ShouldEatWholeUnit(Nutrition(20f), Tomato));

    [Theory]
    [InlineData(3.72f, 1d, false)] // room 0.03 < 1 % of 3.75
    [InlineData(3.70f, 1d, true)] // room 0.05
    [InlineData(0.90f, 75d, true)] // below 25 % of 3.75
    [InlineData(1.00f, 75d, false)]
    [InlineData(3.748f, 0d, false)] // room 0.002: below the vanilla drinking threshold
    [InlineData(3.74f, 0d, true)]
    public void Water_margin_is_against_the_current_hydration_capacity(float hydration, double margin, bool drunk) =>
        Assert.Equal(drunk, Policy(waterMargin: margin).ShouldDrink(new NeedLevel(hydration, 3.75f)));

    [Fact]
    public void The_food_margin_does_not_gate_water() =>
        Assert.True(Policy(foodMargin: 75, waterMargin: 1).ShouldDrink(new NeedLevel(3f, 5f)));
}
