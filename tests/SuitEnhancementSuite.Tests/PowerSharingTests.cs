using Xunit;

namespace SuitEnhancementSuite.Tests;

public class PowerSharePlanTests
{
    private const float Small = 36000f;
    private const float Rate = 500f;

    private static PowerSharingPolicy Policy(float rate = Rate, double loss = 0) => new(rate, Percent.From(loss));

    private static Charge Cell(float stored, float capacity = Small) => new(stored, capacity);

    [Fact]
    public void Each_target_gets_at_most_the_rate_per_tick()
    {
        var plan = new PowerSharePlan();
        var a = plan.AddTarget(ChargePriority.Other, Cell(0f));
        var b = plan.AddTarget(ChargePriority.Other, Cell(1000f));
        plan.AddSource(Small);
        plan.Distribute(Policy());
        Assert.Equal(Rate, plan.DeliveredTo(a));
        Assert.Equal(Rate, plan.DeliveredTo(b));
        Assert.Equal(2 * Rate, plan.DrawnFrom(0));
    }

    [Fact]
    public void A_target_is_filled_only_up_to_its_room()
    {
        var plan = new PowerSharePlan();
        var t = plan.AddTarget(ChargePriority.SuitBattery, Cell(Small - 120f));
        plan.AddSource(Small);
        plan.Distribute(Policy());
        Assert.Equal(120f, plan.DeliveredTo(t));
        Assert.Equal(120f, plan.DrawnFrom(0));
    }

    [Fact]
    public void A_full_target_draws_nothing()
    {
        var plan = new PowerSharePlan();
        plan.AddTarget(ChargePriority.SuitBattery, Cell(Small));
        plan.AddSource(Small);
        plan.Distribute(Policy());
        Assert.Equal(0f, plan.TotalDrawn);
    }

    [Fact]
    public void Suit_battery_is_served_before_hands_before_the_rest_even_when_fuller()
    {
        var plan = new PowerSharePlan();
        var other = plan.AddTarget(ChargePriority.Other, Cell(0f));
        var hand = plan.AddTarget(ChargePriority.Hand, Cell(100f));
        var suit = plan.AddTarget(ChargePriority.SuitBattery, Cell(30000f));
        plan.AddSource(700f);
        plan.Distribute(Policy());
        Assert.Equal(Rate, plan.DeliveredTo(suit));
        Assert.Equal(200f, plan.DeliveredTo(hand));
        Assert.Equal(0f, plan.DeliveredTo(other));
    }

    [Fact]
    public void Within_a_priority_the_lowest_ratio_is_served_first()
    {
        var plan = new PowerSharePlan();
        var fuller = plan.AddTarget(ChargePriority.Other, Cell(18000f)); // 50 %
        var emptier = plan.AddTarget(ChargePriority.Other, Cell(50000f, 288000f)); // 17 %, more joules
        plan.AddSource(300f);
        plan.Distribute(Policy());
        Assert.Equal(300f, plan.DeliveredTo(emptier));
        Assert.Equal(0f, plan.DeliveredTo(fuller));
    }

    [Fact]
    public void The_emptiest_source_is_drained_first()
    {
        var plan = new PowerSharePlan();
        plan.AddTarget(ChargePriority.Other, Cell(0f));
        var full = plan.AddSource(Small);
        var low = plan.AddSource(200f);
        plan.Distribute(Policy());
        Assert.Equal(200f, plan.DrawnFrom(low));
        Assert.Equal(300f, plan.DrawnFrom(full));
    }

    [Fact]
    public void Empty_sources_deliver_nothing()
    {
        var plan = new PowerSharePlan();
        var t = plan.AddTarget(ChargePriority.SuitBattery, Cell(0f));
        plan.AddSource(0f);
        plan.AddSource(-5f);
        plan.AddSource(float.NaN);
        plan.Distribute(Policy());
        Assert.Equal(0f, plan.DeliveredTo(t));
        Assert.Equal(0f, plan.TotalDrawn);
    }

    [Theory]
    [InlineData(0d, 500f, 500f)]
    [InlineData(20d, 500f, 625f)]
    [InlineData(50d, 500f, 1000f)]
    public void Loss_is_drawn_from_the_source_and_never_delivered(double loss, float delivered, float drawn)
    {
        var plan = new PowerSharePlan();
        var t = plan.AddTarget(ChargePriority.Other, Cell(0f));
        plan.AddSource(Small);
        plan.Distribute(Policy(loss: loss));
        Assert.Equal(delivered, plan.DeliveredTo(t), 3);
        Assert.Equal(drawn, plan.DrawnFrom(0), 3);
    }

    [Fact]
    public void With_loss_a_short_source_delivers_its_energy_times_efficiency()
    {
        var plan = new PowerSharePlan();
        var t = plan.AddTarget(ChargePriority.Other, Cell(0f));
        plan.AddSource(100f);
        plan.Distribute(Policy(loss: 25));
        Assert.Equal(75f, plan.DeliveredTo(t), 3);
        Assert.Equal(100f, plan.DrawnFrom(0), 3);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(10d)]
    [InlineData(90d)]
    public void No_energy_is_created_and_no_source_is_overdrawn(double loss)
    {
        var plan = new PowerSharePlan();
        float[] stored = [150f, 900f, 40f];
        for (var i = 0; i < 12; i++)
            plan.AddTarget((ChargePriority)(i % 3), Cell(i * 1000f, i % 2 == 0 ? Small : 1500f));
        foreach (var s in stored) plan.AddSource(s);
        plan.Distribute(Policy(rate: 400f, loss: loss));

        var delivered = 0f;
        for (var t = 0; t < plan.TargetCount; t++) delivered += plan.DeliveredTo(t);
        var drawn = 0f;
        for (var s = 0; s < plan.SourceCount; s++)
        {
            Assert.InRange(plan.DrawnFrom(s), 0f, stored[s] + 0.001f);
            drawn += plan.DrawnFrom(s);
        }
        Assert.True(delivered <= drawn + 0.001f);
        Assert.Equal(drawn * (float)(1 - loss / 100), delivered, 2);
        Assert.Equal(drawn, plan.TotalDrawn, 2);
    }

    [Fact]
    public void Clear_reuses_the_plan_for_the_next_tick()
    {
        var plan = new PowerSharePlan();
        for (var i = 0; i < 40; i++) plan.AddTarget(ChargePriority.Other, Cell(0f));
        plan.AddSource(Small);
        plan.Distribute(Policy());
        plan.Clear();
        var t = plan.AddTarget(ChargePriority.Hand, Cell(0f));
        plan.AddSource(10f);
        plan.Distribute(Policy());
        Assert.Equal(1, plan.TargetCount);
        Assert.Equal(10f, plan.DeliveredTo(t));
    }

    [Fact]
    public void Nothing_moves_without_targets_or_sources()
    {
        var plan = new PowerSharePlan();
        plan.AddSource(Small);
        plan.Distribute(Policy());
        Assert.Equal(0f, plan.TotalDrawn);

        plan.Clear();
        plan.AddTarget(ChargePriority.Other, Cell(0f));
        plan.Distribute(Policy());
        Assert.Equal(0f, plan.TotalDelivered);
    }
}

public class BatteryPlaceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_shared_power_cell_is_never_charged(bool chargeSuitBattery) =>
        Assert.False(BatteryPlace.SharedPower.TryGetPriority(chargeSuitBattery, out _));

    [Fact]
    public void The_suit_battery_is_charged_first_unless_switched_off()
    {
        Assert.True(BatteryPlace.SuitBattery.TryGetPriority(true, out var priority));
        Assert.Equal(ChargePriority.SuitBattery, priority);
        Assert.False(BatteryPlace.SuitBattery.TryGetPriority(false, out _));
    }

    [Fact]
    public void Hands_come_before_the_rest()
    {
        Assert.True(BatteryPlace.Hand.TryGetPriority(false, out var hand));
        Assert.True(BatteryPlace.Elsewhere.TryGetPriority(false, out var elsewhere));
        Assert.Equal(ChargePriority.Hand, hand);
        Assert.Equal(ChargePriority.Other, elsewhere);
        Assert.True(hand < elsewhere);
    }
}
