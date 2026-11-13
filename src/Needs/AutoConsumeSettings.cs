using System.Globalization;
using BepInEx.Configuration;

namespace SuitEnhancementSuite;

/// <summary>
/// The [Auto Consume] section of the config file. Every value is read at use, so edits apply on the next pass.
/// BepInEx clamps each ranged value into its AcceptableValueRange on load and on assignment.
/// </summary>
internal sealed class AutoConsumeSettings
{
    public const string Section = "Auto Consume";

    private static readonly AcceptableValueRange<double> MarginRange = new(0d, 75d);
    private static readonly AcceptableValueRange<double> FallbackRange = new(0d, 50d);
    private static readonly AcceptableValueRange<double> CooldownRange = new(1d, 60d);
    private static readonly AcceptableValueRange<double> WasteThresholdRange = new(25d, 95d);

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<double> _foodMargin;
    private readonly ConfigEntry<double> _waterMargin;
    private readonly ConfigEntry<double> _hungerFallback;
    private readonly ConfigEntry<double> _cooldown;
    private readonly ConfigEntry<double> _wasteThreshold;

    public AutoConsumeSettings(ConfigFile config)
    {
        _enabled = config.Bind(Section, "Enabled", true,
            "On: your suit's Water, Food and Waste slots drink, eat and use the bag for you, and refill from your " +
            "inventory as the Auto Swap settings allow. Off: they are just storage and nothing happens by itself. " +
            "Only the host's game does this in multiplayer.");
        _foodMargin = config.Bind(Section, "Food Top-Off Margin", 1d, new ConfigDescription(
            $"Percent ({Describe(MarginRange)}). Eat from the Food slot once your stomach is at least this percent " +
            "empty. 1 keeps you full all the time; 75 waits until you are below 25 % full.",
            MarginRange));
        _waterMargin = config.Bind(Section, "Water Top-Off Margin", 1d, new ConfigDescription(
            $"Percent ({Describe(MarginRange)}). Drink from the Water slot once you are at least this percent " +
            "short of a full drink. 1 keeps you full all the time; 75 waits until you are below 25 % full.", MarginRange));
        _hungerFallback = config.Bind(Section, "Hunger Fallback", 25d, new ConfigDescription(
            $"Percent ({Describe(FallbackRange)}). A raw crop or cooked vegetable is only eaten when you have room " +
            "for all of it, so none is wasted. A pumpkin fills a whole stomach, so you would wait until you were " +
            "empty. Below this percent of food left, it is eaten anyway and the extra is lost, as when you eat by " +
            "hand. 0 never wastes food.", FallbackRange));
        _cooldown = config.Bind(Section, "Cooldown", 3d, new ConfigDescription(
            $"Seconds ({Describe(CooldownRange)}). Least time between two automatic bites, two drinks or two " +
            "waste-bag uses for the same player.", CooldownRange));
        _wasteThreshold = config.Bind(Section, "Waste Threshold", 25d, new ConfigDescription(
            $"Percent ({Describe(WasteThresholdRange)}). Use the Waste slot's bag once your waste is above this. " +
            "25 is the earliest the game lets you go by hand.", WasteThresholdRange));
    }

    public bool Enabled => _enabled.Value;

    public float CooldownSeconds => (float)_cooldown.Value;

    public ConsumptionPolicy Policy() => new(
        Percent.From(_foodMargin.Value),
        Percent.From(_waterMargin.Value),
        Percent.From(_hungerFallback.Value),
        Percent.From(_wasteThreshold.Value));

    public override string ToString()
    {
        var policy = Policy();
        return $"auto consume {(Enabled ? "on" : "off")}, food margin {policy.FoodMargin}, " +
               $"water margin {policy.WaterMargin}, hunger fallback {policy.HungerFallback}, " +
               $"waste threshold {policy.WasteThreshold}, " +
               $"cooldown {CooldownSeconds.ToString("0.##", CultureInfo.InvariantCulture)} s";
    }

    private static string Describe(AcceptableValueRange<double> range) =>
        string.Format(CultureInfo.InvariantCulture, "{0} to {1}", range.MinValue, range.MaxValue);
}
