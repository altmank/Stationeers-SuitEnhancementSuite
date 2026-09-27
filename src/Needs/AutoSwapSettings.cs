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
                "On: when the worn suit's Water slot is empty or holds an empty container, the fullest water container " +
                "the player carries is moved in, and the empty one goes where it came from (or to a free inventory " +
                "slot). Needs Auto Consume Enabled. Default: on.")),
            (NeedSlot.Food, config.Bind(Section, "Food", true,
                "On: when the worn suit's Food slot is empty or holds something inedible (rotten food), the best food " +
                "the player carries is moved in: the highest food quality first, portioned food before whole crops. " +
                "Needs Auto Consume Enabled. Default: on.")),
            (NeedSlot.Waste, config.Bind(Section, "Waste", true,
                "On: when the worn suit's Waste slot is empty or holds a full bag, an open bag that is not full (the " +
                "fullest first) or one folded bag the player carries is moved in; a full bag goes where the new one " +
                "came from (or to a free inventory slot). Needs Auto Consume Enabled. Default: on.")),
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
