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
