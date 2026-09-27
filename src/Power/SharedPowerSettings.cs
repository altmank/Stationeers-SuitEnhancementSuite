using System.Globalization;
using BepInEx.Configuration;

namespace SuitEnhancementSuite;

/// <summary>
/// The [Shared Power] section of the config file. Every value is read at use, so edits apply on the next power tick.
/// BepInEx clamps each ranged value into its AcceptableValueRange on load and on assignment.
/// </summary>
internal sealed class SharedPowerSettings
{
    public const string Section = "Shared Power";

    private static readonly AcceptableValueRange<double> RateRange = new(10d, 5000d);
    private static readonly AcceptableValueRange<double> LossRange = new(0d, 90d);

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<double> _rate;
    private readonly ConfigEntry<double> _loss;
    private readonly ConfigEntry<bool> _chargeSuitBattery;

    public SharedPowerSettings(ConfigFile config)
    {
        _enabled = config.Bind(Section, "Enabled", true,
            "On: batteries in the worn uniform's two Shared Power slots charge the other batteries you carry " +
            "(host only). Off: the slots only hold batteries. The slots exist either way. Default: on.");
        _rate = config.Bind(Section, "Transfer Rate", 500d, new ConfigDescription(
            $"W per battery ({Describe(RateRange)}): the most each battery receives per power tick (twice a " +
            "second), the unit the game uses for the Battery Cell Charger's 500 W. Default 500.", RateRange));
        _loss = config.Bind(Section, "Loss", 0d, new ConfigDescription(
            $"Percent ({Describe(LossRange)}) of the energy taken from a Shared Power battery that is lost on the " +
            "way. 20 means 100 J taken, 80 J arrive. Default 0.", LossRange));
        _chargeSuitBattery = config.Bind(Section, "Charge Suit Battery", true,
            "On: the worn suit's battery is charged first. Off: the suit battery is left alone and only hands, " +
            "helmet, tools and the rest are charged. Default: on.");
    }

    public bool Enabled => _enabled.Value;

    public bool ChargeSuitBattery => _chargeSuitBattery.Value;

    public PowerSharingPolicy Policy() => new((float)_rate.Value, Percent.From(_loss.Value));

    public override string ToString() =>
        $"shared power {(Enabled ? "on" : "off")}, {Policy()}, suit battery {(ChargeSuitBattery ? "charged" : "skipped")}";

    private static string Describe(AcceptableValueRange<double> range) =>
        string.Format(CultureInfo.InvariantCulture, "{0} to {1}", range.MinValue, range.MaxValue);
}
