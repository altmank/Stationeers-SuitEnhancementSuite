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
            "On: batteries in the two Shared Power slots of your worn Hardsuit or HARM suit charge the other " +
            "batteries you carry. Off: those slots just hold batteries. Only the host's game does this in multiplayer.");
        _rate = config.Bind(Section, "Transfer Rate", 500d, new ConfigDescription(
            $"Watts ({Describe(RateRange)}). The most power each of your other batteries receives from the Shared " +
            "Power batteries every half second. 500 matches the Battery Cell Charger.", RateRange));
        _loss = config.Bind(Section, "Loss", 0d, new ConfigDescription(
            $"Percent ({Describe(LossRange)}) of the power drawn from a Shared Power battery that is lost before it " +
            "reaches the other batteries. 20 means 100 J drawn, 80 J charged.", LossRange));
        _chargeSuitBattery = config.Bind(Section, "Charge Suit Battery", true,
            "On: your suit's own battery is charged first. Off: it is left alone and only the batteries of what " +
            "you hold, your helmet, tools and belt are charged.");
    }

    public bool Enabled => _enabled.Value;

    public bool ChargeSuitBattery => _chargeSuitBattery.Value;

    public PowerSharingPolicy Policy() => new((float)_rate.Value, Percent.From(_loss.Value));

    public override string ToString() =>
        $"shared power {(Enabled ? "on" : "off")}, {Policy()}, suit battery {(ChargeSuitBattery ? "charged" : "skipped")}";

    private static string Describe(AcceptableValueRange<double> range) =>
        string.Format(CultureInfo.InvariantCulture, "{0} to {1}", range.MinValue, range.MaxValue);
}
