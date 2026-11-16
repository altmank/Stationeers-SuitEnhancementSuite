using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SuitEnhancementSuite;

[BepInDependency("stationeers.launchpad", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "net.xceled.stationeers.suitenhancementsuite";
    public const string PluginName = "Suit Enhancement Suite";
    public const string PluginVersion = "0.7.4";

    internal static ManualLogSource Log;

    /// <summary>The one set of extra-log flags: the config fills it at load and the <c>ses log</c> command changes it.</summary>
    internal static readonly VerboseFlags Verbose = new();

    internal static ConfigFile Settings;
    internal static AutoConsumer Consumer;

    private static readonly Dictionary<string, string> LastValues = new();
    private static bool _worldLogged;

    private void Awake()
    {
        Log = Logger;
        Settings = Config;
        var logging = new LoggingSettings(Config);
        var autoConsume = new AutoConsumeSettings(Config);
        var autoSwap = new AutoSwapSettings(Config);
        var sharedPower = new SharedPowerSettings(Config);
        foreach (var entry in Config)
            LastValues[Id(entry.Key)] = entry.Value.GetSerializedValue();
        Config.SettingChanged += (_, e) => LogChange(e, logging);
        logging.Load(Verbose);
        SlotIcons.Load();
        ElectricityTickPatch.SharedPower = new SharedPower(sharedPower);
        var harmony = new Harmony(PluginGuid);
        harmony.PatchAll(typeof(Plugin).Assembly);
        PatchCensus.Check(harmony, typeof(Plugin).Assembly);
        SesCommand.Register();
        Consumer = new AutoConsumer(autoConsume, autoSwap);
        StartCoroutine(AutoConsumeLoop(Consumer));
        StartCoroutine(new HygieneHud().Run());
        Log.LogInfo($"{PluginName} {PluginVersion} loaded: {autoConsume}; {autoSwap}; {sharedPower}");
    }

    private static string Id(ConfigDefinition definition) => definition.Section + "/" + definition.Key;

    /// <summary>One line per setting that changed, with its old and new value.</summary>
    private static void LogChange(SettingChangedEventArgs e, LoggingSettings logging)
    {
        var definition = e.ChangedSetting.Definition;
        var id = Id(definition);
        var now = e.ChangedSetting.GetSerializedValue();
        LastValues.TryGetValue(id, out var before);
        if (before == now) return;
        LastValues[id] = now;
        Log.LogInfo($"Setting {id}: {before ?? "?"} -> {now}");
        if (LoggingSettings.IsVerbose(definition)) logging.Load(Verbose);
    }

    private static IEnumerator AutoConsumeLoop(AutoConsumer consumer)
    {
        var wait = new WaitForSecondsRealtime(AutoConsumer.PassSeconds);
        while (true)
        {
            LogWorldStart();
            consumer.RunPass();
            yield return wait;
        }
    }

    /// <summary>Once per world: the seat, the mods this one depends on or sits beside, and where its work runs.</summary>
    private static void LogWorldStart()
    {
        if (!GameManager.IsRunning)
        {
            _worldLogged = false;
            return;
        }
        if (_worldLogged) return;
        _worldLogged = true;
        try
        {
            Log.LogInfo($"World started as {Seat.Name}; {Seat.Dependencies()}; " +
                        "auto consume and shared power run on the host only" +
                        (Seat.Name == "client" ? " (this seat is a client, so they do nothing here)." : "."));
        }
        catch (Exception e)
        {
            Log.LogWarning($"The world-start summary could not be written ({e.Message}); the mod itself is unaffected.");
        }
    }
}
