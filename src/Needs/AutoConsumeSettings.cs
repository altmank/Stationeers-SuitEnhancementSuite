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

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<double> _foodMargin;
    private readonly ConfigEntry<double> _waterMargin;
    private readonly ConfigEntry<double> _hungerFallback;
    private readonly ConfigEntry<double> _cooldown;

    public AutoConsumeSettings(ConfigFile config)
    {
        _enabled = config.Bind(Section, "Enabled", true,
            "On: the Water and Food slots of the worn suit are eaten and drunk from automatically (host only). " +
            "Off: both slots are storage only. The slots exist either way. Default: on.");
        _foodMargin = config.Bind(Section, "Food Top-Off Margin", 1d, new ConfigDescription(
            $"Percent ({Describe(MarginRange)}). Eat from the Food slot once at least this share of the stomach " +
            "(nutrition capacity) is empty. Default 1 keeps the player topped off; 75 eats only below 25 %.",
            MarginRange));
        _waterMargin = config.Bind(Section, "Water Top-Off Margin", 1d, new ConfigDescription(
            $"Percent ({Describe(MarginRange)}). Drink from the Water slot once at least this share of the current " +
            "hydration capacity (which varies with food quality) is empty. Default 1 keeps the player topped off; " +
            "75 drinks only below 25 %.", MarginRange));
        _hungerFallback = config.Bind(Section, "Hunger Fallback", 25d, new ConfigDescription(
            $"Percent of nutrition ({Describe(FallbackRange)}). Whole-unit food (raw crops, cooked vegetables) is " +
            "eaten only when all of its nutrition fits, so nothing is wasted; a pumpkin fills the whole stomach, so " +
            "it would fit only when empty. Below this nutrition level a whole unit is eaten anyway and the overflow " +
            "is lost, exactly as when eaten by hand. 0 never wastes. Default 25.", FallbackRange));
        _cooldown = config.Bind(Section, "Cooldown", 3d, new ConfigDescription(
            $"Seconds ({Describe(CooldownRange)}). Minimum real time between two automatic bites of the same need " +
            "(food or water) for the same player. Default 3.", CooldownRange));
    }

    public bool Enabled => _enabled.Value;

    public float CooldownSeconds => (float)_cooldown.Value;

    public ConsumptionPolicy Policy() => new(
        Percent.From(_foodMargin.Value),
        Percent.From(_waterMargin.Value),
        Percent.From(_hungerFallback.Value));

    public override string ToString()
    {
        var policy = Policy();
        return $"auto consume {(Enabled ? "on" : "off")}, food margin {policy.FoodMargin}, " +
               $"water margin {policy.WaterMargin}, hunger fallback {policy.HungerFallback}, " +
               $"cooldown {CooldownSeconds.ToString("0.##", CultureInfo.InvariantCulture)} s";
    }

    private static string Describe(AcceptableValueRange<double> range) =>
        string.Format(CultureInfo.InvariantCulture, "{0} to {1}", range.MinValue, range.MaxValue);
}
