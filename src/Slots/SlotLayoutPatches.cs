using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using HarmonyLib;

namespace SuitEnhancementSuite;

// Every entry point that can build or show clothing slots adds the extra slots first. SuitSlotLayout.AddSlots is
// idempotent, so the overlap is harmless.

/// <summary>Adds the slots before the clothing initialises its inventory-window cache.</summary>
[HarmonyPatch(typeof(Thing), "Awake")]
internal static class ThingAwakeSlotsPatch
{
    private static void Prefix(Thing __instance)
    {
        if (__instance is not DynamicThing clothing) return;
        if (SuitSlotLayout.AddSlots(clothing))
            Plugin.Log.LogDebug($"Added suit slots to {clothing.PrefabName}.");
    }
}

/// <summary>
/// A saved item names its parent slot by index and is moved there without a CanEnter check, so the slots must exist
/// before the clothing's saved children are restored.
/// </summary>
[HarmonyPatch(typeof(DynamicThing), "DeserializeSave", new[] { typeof(ThingSaveData) })]
internal static class DeserializeSaveSlotsPatch
{
    private static void Prefix(DynamicThing __instance) => SuitSlotLayout.AddSlots(__instance);
}

[HarmonyPatch(typeof(InventoryWindow), "SetSlots")]
internal static class InventoryWindowSetSlotsLayoutPatch
{
    private static void Prefix(InventoryWindow __instance)
    {
        if (__instance?.Parent is DynamicThing parent)
            SuitSlotLayout.AddSlots(parent);
    }
}

/// <summary>Assign opens every clothing inventory, including suit classes that never rebuild through SetSlots.</summary>
[HarmonyPatch(typeof(InventoryWindow), "Assign")]
internal static class InventoryWindowAssignLayoutPatch
{
    private static void Prefix(Slot parentSlot)
    {
        if (parentSlot?.Get() is DynamicThing parent)
            SuitSlotLayout.AddSlots(parent);
    }
}
