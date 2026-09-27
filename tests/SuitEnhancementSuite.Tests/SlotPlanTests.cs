using System.Linq;
using Xunit;

namespace SuitEnhancementSuite.Tests;

public class SlotPlanTests
{
    private static string[] Keys(Garment garment) => SlotPlan.For(garment).Select(slot => slot.Key).ToArray();

    private static readonly string[] NeedSlots = [SlotKeys.Water, SlotKeys.Food, SlotKeys.Waste];

    [Fact]
    public void A_uniform_gets_four_storage_slots_and_no_shared_power() =>
        Assert.Equal(Enumerable.Range(1, 4).Select(SlotKeys.UniformStorage), Keys(Garment.Uniform));

    [Fact]
    public void A_suit_gets_water_food_and_waste_in_that_order() => Assert.Equal(NeedSlots, Keys(Garment.Suit));

    [Fact]
    public void An_advanced_suit_gets_the_suit_slots_then_two_shared_power_slots() =>
        Assert.Equal([.. NeedSlots, SlotKeys.FirstSharedPower, SlotKeys.SecondSharedPower], Keys(Garment.AdvancedSuit));

    [Fact]
    public void Body_armor_gets_eight_storage_slots_then_the_suit_slots() =>
        Assert.Equal([.. Enumerable.Range(1, 8).Select(SlotKeys.ArmorStorage), .. NeedSlots], Keys(Garment.BodyArmor));

    // Saves store items by slot index: an advanced suit saved as a plain suit (or before 0.7.0) keeps every index.
    [Fact]
    public void The_advanced_suit_plan_only_extends_the_suit_plan() =>
        Assert.Equal(Keys(Garment.Suit), Keys(Garment.AdvancedSuit).Take(Keys(Garment.Suit).Length));

    [Fact]
    public void Only_shared_power_slots_are_battery_slots()
    {
        foreach (var garment in new[] { Garment.Uniform, Garment.BodyArmor, Garment.Suit, Garment.AdvancedSuit })
            foreach (var slot in SlotPlan.For(garment))
                Assert.Equal(SlotKeys.IsSharedPower(slot.Key), slot.Kind == AddedSlotKind.SharedPower);
    }

    [Fact]
    public void Every_garment_has_distinct_keys()
    {
        foreach (var garment in new[] { Garment.Uniform, Garment.BodyArmor, Garment.Suit, Garment.AdvancedSuit })
            Assert.Equal(Keys(garment).Length, Keys(garment).Distinct().Count());
    }
}
