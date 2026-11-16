using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SuitEnhancementSuite.Tests;

public sealed class VerboseFlagsTests
{
    private static readonly LogArea[] Areas = [LogArea.Consume, LogArea.Swap, LogArea.Power, LogArea.Slots, LogArea.Hud];

    private static VerboseFlags Applied(string spec)
    {
        var flags = new VerboseFlags();
        Assert.True(flags.TryApply(spec, out _));
        return flags;
    }

    [Fact]
    public void A_new_set_and_off_have_no_area_on()
    {
        Assert.Equal("off", new VerboseFlags().Summary());
        Assert.Equal("off", Applied("off").Summary());
        Assert.Equal("off", Applied("").Summary());
    }

    [Fact]
    public void All_turns_every_area_on()
    {
        var flags = Applied("all");
        foreach (var area in Areas) Assert.True(flags.On(area));
    }

    [Fact]
    public void A_list_ignores_case_and_spaces()
    {
        var flags = Applied(" Consume , SWAP ");
        Assert.Equal("consume,swap", flags.Summary());
    }

    [Fact]
    public void A_bad_name_is_refused_with_the_valid_names_and_changes_nothing()
    {
        var flags = Applied("power");

        var accepted = flags.TryApply("consume,food", out var refusal);

        Assert.False(accepted);
        Assert.Contains("food", refusal);
        foreach (var name in new[] { "off", "all", "consume", "swap", "power", "slots", "hud" })
            Assert.Contains(name, refusal);
        Assert.Equal("power", flags.Summary());
    }

    [Fact]
    public void The_console_changes_the_set_the_config_filled()
    {
        var flags = Applied("consume,swap");

        Assert.True(flags.TrySet("swap", false, out _));

        Assert.Equal("consume", flags.Summary());
        Assert.True(flags.TrySet("ALL", true, out _));
        Assert.Equal("consume,swap,power,slots,hud", flags.Summary());
    }

    [Fact]
    public void The_console_refuses_a_bad_area_and_names_the_valid_ones()
    {
        var flags = new VerboseFlags();

        Assert.False(flags.TrySet("food", true, out var refusal));

        Assert.Contains("valid: off, all, consume, swap, power, slots, hud", refusal);
        Assert.Equal("off", flags.Summary());
    }

    [Fact]
    public void The_list_shows_every_area_with_its_state()
    {
        var lines = Applied("hud").ListLines();

        Assert.Equal("consume: off\nswap: off\npower: off\nslots: off\nhud: on", lines);
    }

    [Fact]
    public void Checking_an_area_with_everything_off_allocates_nothing()
    {
        var flags = new VerboseFlags();
        var seen = 0;
        for (var i = 0; i < 100; i++) seen += flags.On(LogArea.Consume) ? 1 : 0;

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
            foreach (var area in Areas)
                seen += flags.On(area) ? 1 : 0;
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, seen);
        Assert.Equal(before, after);
    }

    [Fact]
    public void Things_are_named_prefab_hash_id_and_numbers_ignore_the_language()
    {
        var saved = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("ItemCerealBar#1234567", LogText.Thing("ItemCerealBar", 1234567));
            Assert.Equal("?#5", LogText.Thing(null, 5));
            Assert.Equal("20.50", LogText.Number(20.5f, 2));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = saved;
        }
    }
}
