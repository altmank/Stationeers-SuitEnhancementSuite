using System.Collections;
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
    public const string PluginVersion = "0.6.0";

    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        var autoConsume = new AutoConsumeSettings(Config);
        var autoSwap = new AutoSwapSettings(Config);
        var sharedPower = new SharedPowerSettings(Config);
        Config.SettingChanged += (_, e) => LogChange(e, autoConsume, autoSwap, sharedPower);
        SlotIcons.Load();
        ElectricityTickPatch.SharedPower = new SharedPower(sharedPower);
        new Harmony(PluginGuid).PatchAll(typeof(Plugin).Assembly);
        StartCoroutine(AutoConsumeLoop(new AutoConsumer(autoConsume, autoSwap)));
        StartCoroutine(new HygieneHud().Run());
        Log.LogInfo($"{PluginName} {PluginVersion} loaded: {autoConsume}; {autoSwap}; {sharedPower}");
    }

    private static void LogChange(SettingChangedEventArgs e, AutoConsumeSettings autoConsume, AutoSwapSettings autoSwap,
        SharedPowerSettings sharedPower)
    {
        var section = e.ChangedSetting.Definition.Section;
        if (section == AutoConsumeSettings.Section) Log.LogInfo($"Settings changed: {autoConsume}");
        else if (section == AutoSwapSettings.Section) Log.LogInfo($"Settings changed: {autoSwap}");
        else if (section == SharedPowerSettings.Section) Log.LogInfo($"Settings changed: {sharedPower}");
    }

    private static IEnumerator AutoConsumeLoop(AutoConsumer consumer)
    {
        var wait = new WaitForSecondsRealtime(AutoConsumer.PassSeconds);
        while (true)
        {
            consumer.RunPass();
            yield return wait;
        }
    }
}
