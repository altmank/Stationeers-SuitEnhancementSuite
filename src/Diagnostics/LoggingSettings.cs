using BepInEx.Configuration;

namespace SuitEnhancementSuite;

/// <summary>
/// The [Logging] section of the config file: which areas write extra log lines. The default keeps the log quiet. The
/// console command changes the flags for this session only and never writes this entry.
/// </summary>
internal sealed class LoggingSettings
{
    public const string Section = "Logging";
    public const string Key = "Verbose";
    public const string Default = "off";

    private readonly ConfigEntry<string> _verbose;

    public LoggingSettings(ConfigFile config)
    {
        _verbose = config.Bind(Section, Key, Default,
            "Extra log lines for finding problems: off, all, or a comma list of consume, swap, power, slots, hud. " +
            "Off keeps the log quiet. Power adds no log lines yet; it only adds to the ses diag report.");
    }

    public string Value => _verbose.Value;

    /// <summary>Whether a changed setting is this one.</summary>
    public static bool IsVerbose(ConfigDefinition definition) => definition.Section == Section && definition.Key == Key;

    /// <summary>Fills <paramref name="flags"/> from the config value. A bad value is logged and the flags stay as they were.</summary>
    public void Load(VerboseFlags flags)
    {
        if (flags.TryApply(_verbose.Value, out var refusal))
            Plugin.Log.LogInfo($"Extra log lines: {flags.Summary()}");
        else
            Plugin.Log.LogWarning($"[{Section}] {Key} '{_verbose.Value}' ignored: {refusal}; extra log lines stay {flags.Summary()}.");
    }
}
