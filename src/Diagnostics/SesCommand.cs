using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Util.Commands;

namespace SuitEnhancementSuite;

/// <summary>
/// The <c>ses</c> console command: <c>ses</c> prints help, <c>ses log</c> lists and changes the extra log lines for this
/// session, <c>ses diag [area]</c> writes a state block to the BepInEx log. A verb never throws.
/// </summary>
internal sealed class SesCommand : CommandBase
{
    public const string Key = "ses";

    public override string HelpText =>
        "ses: Suit Enhancement Suite diagnostics.\n" +
        "ses diag [area]: write the mod's state to the BepInEx log\n" +
        "ses log: list the extra log areas | ses log <area>|all on|off: change them for this session\n" +
        "areas: " + VerboseFlags.ValidNames;

    public override string[] Arguments => ["diag|log", "..."];

    public override bool IsLaunchCmd => false;

    /// <summary>Adds the command to the game's console once per process.</summary>
    public static void Register()
    {
        if (!CommandLine.CommandsMap.ContainsKey(Key))
            CommandLine.AddCommand(Key, new SesCommand());
    }

    public override string Execute(string[] args)
    {
        if (args.Length == 0) return HelpText;
        var verb = args[0].ToLowerInvariant();
        try
        {
            return verb switch
            {
                "diag" => Diag(args),
                "log" => Log(args),
                _ => HelpText,
            };
        }
        catch (Exception e)
        {
            return $"ses {verb} failed: {e.Message}";
        }
    }

    private static string Log(string[] args)
    {
        var flags = Plugin.Verbose;
        if (args.Length == 1) return flags.ListLines();
        var on = args.Length == 3 && args[2].Equals("on", StringComparison.OrdinalIgnoreCase);
        var off = args.Length == 3 && args[2].Equals("off", StringComparison.OrdinalIgnoreCase);
        if (!on && !off) return "usage: ses log | ses log <area>|all on|off";
        if (!flags.TrySet(args[1], on, out var refusal)) return $"ses log: {refusal}";
        Plugin.Log.LogInfo($"Extra log lines for this session: {flags.Summary()}");
        return $"extra log lines: {flags.Summary()} (this session only; the config file is unchanged)";
    }

    private static string Diag(string[] args)
    {
        LogArea? only = null;
        if (args.Length > 1 && !args[1].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (!VerboseFlags.TryParseArea(args[1], out var area, out var refusal)) return $"ses diag: {refusal}";
            only = area;
        }
        var text = new StringBuilder();
        text.Append("ses diag: ").Append(Plugin.PluginName).Append(' ').Append(Plugin.PluginVersion)
            .Append(", seat ").Append(Seat.Name).Append('\n');
        text.Append("  dependencies: ").Append(Seat.Dependencies()).Append('\n');
        text.Append("  patches: ").Append(PatchCensus.Summary).Append('\n');
        text.Append("  config differing from defaults: ").Append(NonDefaultConfig()).Append('\n');
        text.Append("  extra log lines: ").Append(Plugin.Verbose.Summary());
        if (only is null or LogArea.Consume or LogArea.Swap) DescribeNeeds(text);
        if (only is null or LogArea.Power) DescribePower(text);
        if (only is null or LogArea.Slots) text.Append("\n  slots: ").Append(ThingAwakeSlotsPatch.Added)
            .Append(" clothing items got SES slots when they were created (items restored from a save are not counted)");
        if (only is null or LogArea.Hud) DescribeHud(text);
        var block = text.ToString();
        Plugin.Log.LogInfo(block);
        return $"ses diag: {Lines(block)} lines in the BepInEx log";
    }

    private static void DescribeNeeds(StringBuilder text)
    {
        text.Append("\n  consume and swap: ");
        if (!GameManager.RunSimulation) text.Append("run on the host only; this seat is a client");
        else Plugin.Consumer?.Describe(text);
    }

    private static void DescribePower(StringBuilder text)
    {
        text.Append("\n  power: ");
        if (!GameManager.RunSimulation) text.Append("runs on the host only; this seat is a client");
        else ElectricityTickPatch.SharedPower?.Describe(text);
    }

    private static void DescribeHud(StringBuilder text)
    {
        text.Append("\n  hud: ");
        text.Append(GameManager.IsBatchMode ? "a dedicated server has no HUD" : "row path: " + HygieneHud.Path);
    }

    private static string NonDefaultConfig()
    {
        var text = new StringBuilder();
        foreach (var pair in Plugin.Settings)
        {
            var entry = pair.Value;
            var now = entry.GetSerializedValue();
            var standard = TomlTypeConverter.ConvertToString(entry.DefaultValue, entry.SettingType);
            if (now == standard) continue;
            text.Append(text.Length == 0 ? "" : "; ").Append(pair.Key.Section).Append('/').Append(pair.Key.Key)
                .Append(" = ").Append(now).Append(" (default ").Append(standard).Append(')');
        }
        return text.Length == 0 ? "none" : text.ToString();
    }

    private static int Lines(string block)
    {
        var count = 1;
        foreach (var c in block)
            if (c == '\n') count++;
        return count;
    }
}

/// <summary>Which seat this game is, and which mods it runs beside.</summary>
internal static class Seat
{
    public static string Name => GameManager.IsBatchMode ? "dedicated server" : GameManager.RunSimulation ? "host" : "client";

    public static string Dependencies() =>
        $"StationeersLaunchPad {Version("stationeers.launchpad")}, StationeersLua {Version("zedle.stationeers.lua")}, " +
        $"Aegis {Version("net.xceled.stationeers.aegis")}";

    private static string Version(string guid) =>
        Chainloader.PluginInfos.TryGetValue(guid, out var info) ? info.Metadata.Version.ToString() : "not loaded";
}

/// <summary>Names a thing the way every log line does: <c>PrefabName#ReferenceId</c>.</summary>
internal static class LogNames
{
    public static string Of(Thing thing) => thing is null ? "nothing" : LogText.Thing(thing.PrefabName, thing.ReferenceId);
}

/// <summary>Counts the Harmony patch methods this mod installed against the ones it declares, and names any missing.</summary>
internal static class PatchCensus
{
    private static readonly string[] PatchMethodNames = ["Prefix", "Postfix", "Transpiler", "Finalizer"];

    public static string Summary { get; private set; } = "not checked yet";

    public static void Check(Harmony harmony, Assembly assembly)
    {
        try
        {
            var installed = new HashSet<int>();
            foreach (var target in harmony.GetPatchedMethods())
            {
                var info = Harmony.GetPatchInfo(target);
                if (info is null) continue;
                AddAll(installed, info.Prefixes);
                AddAll(installed, info.Postfixes);
                AddAll(installed, info.Transpilers);
                AddAll(installed, info.Finalizers);
            }
            var expected = 0;
            var missing = new List<string>();
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsDefined(typeof(HarmonyPatch), false)) continue;
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (Array.IndexOf(PatchMethodNames, method.Name) < 0) continue;
                    expected++;
                    if (!installed.Contains(method.MetadataToken)) missing.Add(type.Name + "." + method.Name);
                }
            }
            Summary = $"{expected - missing.Count} of {expected} installed" + (missing.Count == 0 ? "" : "; failed: " + string.Join(", ", missing));
            if (missing.Count == 0) Plugin.Log.LogInfo($"Harmony patches: {Summary}.");
            else Plugin.Log.LogWarning($"Harmony patches: {Summary}. Those features do not work until the game restarts without the cause.");
        }
        catch (Exception e)
        {
            Summary = "check failed: " + e.Message;
            Plugin.Log.LogWarning($"Harmony patch count failed ({e.Message}); the patches themselves are unaffected.");
        }
    }

    private static void AddAll(HashSet<int> installed, IEnumerable<Patch> patches)
    {
        foreach (var patch in patches)
            installed.Add(patch.PatchMethod.MetadataToken);
    }
}
