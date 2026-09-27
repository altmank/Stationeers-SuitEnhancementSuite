using Assets.Scripts.Networks;
using HarmonyLib;

namespace SuitEnhancementSuite;

/// <summary>
/// Shares power after the game's power tick: every cable network and every battery has ticked, so the transfers land
/// on the values the game has just drained, and the next tick shows and syncs them.
/// </summary>
[HarmonyPatch(typeof(ElectricityManager), nameof(ElectricityManager.ElectricityTick))]
internal static class ElectricityTickPatch
{
    /// <summary>Set once at plugin start, before patching.</summary>
    internal static SharedPower SharedPower;

    private static void Postfix() => SharedPower?.Tick();
}
