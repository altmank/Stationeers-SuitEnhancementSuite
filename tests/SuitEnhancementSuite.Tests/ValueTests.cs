using Xunit;

namespace SuitEnhancementSuite.Tests;

public class PercentTests
{
    [Theory]
    [InlineData(-5d, 0d)]
    [InlineData(double.NaN, 0d)]
    [InlineData(double.PositiveInfinity, 100d)]
    [InlineData(250d, 100d)]
    [InlineData(0.1d, 0.1d)]
    public void Out_of_range_values_are_clamped(double input, double expected) =>
        Assert.Equal(expected, Percent.From(input).Value);

    [Fact]
    public void Of_takes_the_share_of_a_whole() => Assert.Equal(12.5d, Percent.From(25).Of(50), 6);

    [Fact]
    public void Formats_invariantly() => Assert.Equal("1.5 %", Percent.From(1.5).ToString());
}

public class NeedCooldownTests
{
    [Fact]
    public void A_player_is_ready_until_served_then_after_the_cooldown()
    {
        var cooldown = new NeedCooldown();
        Assert.True(cooldown.IsReady(7, 100f));
        cooldown.Start(7, 100f, 3f);
        Assert.False(cooldown.IsReady(7, 102.9f));
        Assert.True(cooldown.IsReady(7, 103f));
        Assert.True(cooldown.IsReady(8, 101f));
    }

    [Fact]
    public void Prune_drops_only_expired_entries()
    {
        var cooldown = new NeedCooldown();
        cooldown.Start(1, 0f, 3f);
        cooldown.Start(2, 0f, 60f);
        cooldown.Prune(10f);
        Assert.Equal(1, cooldown.Count);
        Assert.False(cooldown.IsReady(2, 10f));
    }
}

public class TickGateTests
{
    [Fact]
    public void A_player_is_open_until_served_then_after_two_completed_game_ticks()
    {
        var gate = new TickGate();
        Assert.True(gate.IsOpen(7, 100));
        gate.Close(7, 100);
        Assert.False(gate.IsOpen(7, 100));
        Assert.False(gate.IsOpen(7, 101));
        Assert.True(gate.IsOpen(7, 102));
        Assert.True(gate.IsOpen(8, 100));
    }

    [Fact]
    public void A_paused_game_keeps_the_gate_closed_however_much_real_time_passes()
    {
        var gate = new TickGate();
        gate.Close(7, 100);
        for (var pass = 0; pass < 1000; pass++)
            Assert.False(gate.IsOpen(7, 100));
    }

    [Fact]
    public void A_restarted_or_wrapped_tick_counter_reads_as_settled()
    {
        var gate = new TickGate();
        gate.Close(1, 5000);
        Assert.True(gate.IsOpen(1, 3));
        gate.Close(2, uint.MaxValue);
        Assert.False(gate.IsOpen(2, 0));
        Assert.True(gate.IsOpen(2, 1));
    }

    [Fact]
    public void Prune_drops_only_settled_entries()
    {
        var gate = new TickGate();
        gate.Close(1, 10);
        gate.Close(2, 19);
        gate.Prune(20);
        Assert.Equal(1, gate.Count);
        Assert.False(gate.IsOpen(2, 20));
    }
}
