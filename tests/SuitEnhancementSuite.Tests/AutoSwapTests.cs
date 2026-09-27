using Xunit;

namespace SuitEnhancementSuite.Tests;

public class ReplacementRankingTests
{
    // Walks the offers in inventory order, as the game side does, and names the winner.
    private static string Pick<TOffer>(params (string Name, TOffer Offer)[] inventory) where TOffer : struct, IOffer<TOffer>
    {
        var best = new BestOffer<TOffer, string>();
        foreach (var (name, offer) in inventory)
            best.Consider(name, offer);
        return best.Item;
    }

    [Fact]
    public void Nothing_offered_picks_nothing() => Assert.Null(Pick<WaterOffer>());

    [Fact]
    public void Water_the_container_holding_the_most_water_wins() =>
        Assert.Equal("full", Pick(("half", new WaterOffer(0.5f)), ("full", new WaterOffer(1.5f)), ("low", new WaterOffer(0.1f))));

    [Fact]
    public void Water_ties_go_to_the_first_found() =>
        Assert.Equal("first", Pick(("first", new WaterOffer(1f)), ("second", new WaterOffer(1f))));

    [Fact]
    public void Food_the_higher_quality_tier_wins_over_everything_else() =>
        Assert.Equal("canned", Pick(
            ("raw tomato", FoodOffer.WholeUnit(FoodGrade.Raw, 15f)),
            ("cooked bar", FoodOffer.Portioned(FoodGrade.Cooked, 100f)),
            ("canned", FoodOffer.Portioned(FoodGrade.Canned, 5f)),
            ("none", FoodOffer.Portioned(FoodGrade.None, 500f))));

    [Fact]
    public void Food_complex_beats_canned() =>
        Assert.Equal("complex", Pick(("canned", FoodOffer.Portioned(FoodGrade.Canned, 50f)), ("complex", FoodOffer.WholeUnit(FoodGrade.Complex, 50f))));

    [Fact]
    public void Food_in_the_same_tier_portioned_beats_whole_units() =>
        Assert.Equal("portion", Pick(("whole", FoodOffer.WholeUnit(FoodGrade.Cooked, 1f)), ("portion", FoodOffer.Portioned(FoodGrade.Cooked, 1f))));

    [Fact]
    public void Food_whole_units_the_smallest_unit_wins_so_it_fits_soonest() =>
        Assert.Equal("tomato", Pick(("pumpkin", FoodOffer.WholeUnit(FoodGrade.Raw, 50f)), ("tomato", FoodOffer.WholeUnit(FoodGrade.Raw, 15f))));

    [Fact]
    public void Food_portioned_the_most_nutrition_left_wins() =>
        Assert.Equal("full can", Pick(("half can", FoodOffer.Portioned(FoodGrade.Canned, 20f)), ("full can", FoodOffer.Portioned(FoodGrade.Canned, 40f))));

    [Fact]
    public void Food_ties_go_to_the_first_found() =>
        Assert.Equal("first", Pick(("first", FoodOffer.WholeUnit(FoodGrade.Raw, 15f)), ("second", FoodOffer.WholeUnit(FoodGrade.Raw, 15f))));

    [Fact]
    public void Waste_an_open_bag_beats_folded_bags() =>
        Assert.Equal("open", Pick(("folded", WasteOffer.Folded), ("open", WasteOffer.Open(0.1f))));

    [Fact]
    public void Waste_the_fullest_open_bag_wins() =>
        Assert.Equal("fuller", Pick(("emptier", WasteOffer.Open(0.2f)), ("fuller", WasteOffer.Open(0.7f)), ("folded", WasteOffer.Folded)));

    [Fact]
    public void Waste_folded_bags_tie_to_the_first_found() =>
        Assert.Equal("first", Pick(("first", WasteOffer.Folded), ("second", WasteOffer.Folded)));

    [Fact]
    public void A_cleared_pick_forgets_the_previous_walk()
    {
        var best = new BestOffer<WaterOffer, string>();
        best.Consider("old", new WaterOffer(5f));
        best.Clear();
        best.Consider("new", new WaterOffer(0.1f));
        Assert.Equal("new", best.Item);
    }
}

public class SwapDecisionTests
{
    // xunit theories are public, the plan types internal: a split flag stands for the replacement shape.
    private static ReplacementShape Shape(bool split) => split ? ReplacementShape.OneOfStack : ReplacementShape.Whole;

    [Fact]
    public void An_empty_slot_takes_a_whole_replacement_directly() =>
        Assert.Equal(SwapPlan.MoveIn, SwapDecision.Decide(SlotContent.Empty, Shape(split: false), sourceTakesSpent: false, hasFreeSlot: false));

    [Fact]
    public void An_empty_slot_takes_one_bag_split_off_a_stack() =>
        Assert.Equal(SwapPlan.SplitIn, SwapDecision.Decide(SlotContent.Empty, Shape(split: true), sourceTakesSpent: false, hasFreeSlot: false));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_usable_item_is_kept(bool split) =>
        Assert.Equal(SwapPlan.Keep, SwapDecision.Decide(SlotContent.Usable, Shape(split), sourceTakesSpent: true, hasFreeSlot: true));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_spent_item_swaps_back_into_the_source_slot_when_it_fits(bool hasFreeSlot) =>
        Assert.Equal(SwapPlan.SwapBack, SwapDecision.Decide(SlotContent.Spent, Shape(split: false), sourceTakesSpent: true, hasFreeSlot));

    [Fact]
    public void A_spent_item_the_source_cannot_take_goes_to_a_free_slot() =>
        Assert.Equal(SwapPlan.StowThenMoveIn, SwapDecision.Decide(SlotContent.Spent, Shape(split: false), sourceTakesSpent: false, hasFreeSlot: true));

    [Fact]
    public void A_split_leaves_the_source_occupied_so_the_spent_item_needs_a_free_slot() =>
        Assert.Equal(SwapPlan.StowThenSplitIn, SwapDecision.Decide(SlotContent.Spent, Shape(split: true), sourceTakesSpent: true, hasFreeSlot: true));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)] // the source would take it, but a split never empties the source
    [InlineData(true, false)]
    public void No_room_for_the_spent_item_blocks_the_swap(bool split, bool sourceTakesSpent) =>
        Assert.Equal(SwapPlan.Blocked, SwapDecision.Decide(SlotContent.Spent, Shape(split), sourceTakesSpent, hasFreeSlot: false));
}

public class ReportLatchTests
{
    [Fact]
    public void A_condition_is_reported_once_per_player_until_cleared()
    {
        var latch = new ReportLatch();
        Assert.True(latch.TryRaise(1));
        Assert.False(latch.TryRaise(1));
        Assert.True(latch.TryRaise(2));
        latch.Clear(1);
        Assert.True(latch.TryRaise(1));
    }
}
