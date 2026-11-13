using BepInEx.Configuration;

namespace SuitEnhancementSuite;

/// <summary>The [Auto Swap] section of the config file. Every value is read at use, so edits apply on the next pass.</summary>
internal sealed class AutoSwapSettings
{
    public const string Section = "Auto Swap";

    private readonly (NeedSlot Need, ConfigEntry<bool> Entry)[] _toggles;

    public AutoSwapSettings(ConfigFile config)
    {
        _toggles =
        [
            (NeedSlot.Water, config.Bind(Section, "Water", true,
                "On: when your suit's Water slot is empty or holds an empty bottle, the fullest bottle you carry " +
                "(not in your hands) moves in, and the empty one goes back where the new one came from, or into a " +
                "free slot. Works only while Auto Consume Enabled is on.")),
            (NeedSlot.Food, config.Bind(Section, "Food", true,
                "On: when your suit's Food slot is empty or holds rotten food, the best food you carry (not in your " +
                "hands) moves in: meals first, then canned food, cooked vegetables, raw crops. " +
                "Works only while Auto Consume Enabled is on.")),
            (NeedSlot.Waste, config.Bind(Section, "Waste", true,
                "On: when your suit's Waste slot is empty or holds a full bag, a part-filled bag you carry (the " +
                "fullest first) or else a folded Waste Bag moves in; the full bag goes back where the new one came " +
                "from, or into a free slot. Works only while Auto Consume Enabled is on.")),
        ];
    }

    public bool IsOn(NeedSlot need)
    {
        foreach (var toggle in _toggles)
            if (toggle.Need == need)
                return toggle.Entry.Value;
        return false;
    }

    public override string ToString() =>
        $"auto swap water {OnOff(NeedSlot.Water)}, food {OnOff(NeedSlot.Food)}, waste {OnOff(NeedSlot.Waste)}";

    private string OnOff(NeedSlot need) => IsOn(need) ? "on" : "off";
}
