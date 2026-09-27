using Assets.Scripts.Objects;
using HarmonyLib;
using GameStrings = Assets.Scripts.Localization2.GameStrings;

namespace SuitEnhancementSuite;

// The Water, Food and Waste slots are of class None, which vanilla lets anything enter. The inventory UI checks
// IsAllowedType and AllowMove before it asks the server to move, so those two admit exactly what NeedSlot.Accepts admits.
// The server's move (DynamicThing.MoveToSlot) checks only CanEnter, and so does loading a save: each item is moved back
// into its saved slot index and left in the world when refused. CanEnter therefore admits what NeedSlot.Holds admits,
// so a bag that filled up in the Waste slot is still there after a reload, and a refused swap can put it back. The UI's
// slot swap (AllowSwap) checks only CanEnter as well, so it is held to NeedSlot.Accepts here too.

[HarmonyPatch(typeof(Slot), "IsAllowedType", new[] { typeof(DynamicThing) })]
internal static class NeedSlotAllowedTypePatch
{
    private static bool Prefix(Slot __instance, DynamicThing dynamicThing, ref bool __result)
    {
        if (__instance == null || dynamicThing == null || !NeedSlot.TryGet(__instance.StringKey, out var need)) return true;
        __result = need.Accepts(dynamicThing);
        return false;
    }
}

[HarmonyPatch(typeof(Slot), "AllowMove", new[] { typeof(DynamicThing), typeof(Slot) })]
internal static class NeedSlotAllowMovePatch
{
    private static bool Prefix(DynamicThing thing, Slot destinationSlot, ref bool __result)
    {
        if (thing == null || destinationSlot == null || !NeedSlot.TryGet(destinationSlot.StringKey, out var need)) return true;
        __result = need.Accepts(thing);
        return false;
    }
}

[HarmonyPatch(typeof(Thing), "CanEnter", new[] { typeof(Slot) })]
internal static class NeedSlotCanEnterPatch
{
    private static void Postfix(Thing __instance, Slot destinationSlot, ref CanEnterResult __result)
    {
        if (__instance == null || destinationSlot == null || !NeedSlot.TryGet(destinationSlot.StringKey, out var need)) return;
        if (need.Holds(__instance))
            __result = CanEnterResult.Succeed;
        else if (__result)
            __result = CanEnterResult.Fail(GameStrings.ThingIsNotType, __instance.DisplayName, destinationSlot.DisplayName);
    }
}

[HarmonyPatch(typeof(Slot), "AllowSwap", new[] { typeof(Slot), typeof(Slot) })]
internal static class NeedSlotAllowSwapPatch
{
    private static void Postfix(Slot sourceSlot, Slot destinationSlot, ref bool __result)
    {
        if (__result && (Refuses(destinationSlot, sourceSlot) || Refuses(sourceSlot, destinationSlot))) __result = false;
    }

    // A swap puts the occupant of the other slot into this one.
    private static bool Refuses(Slot slot, Slot other) =>
        slot != null && NeedSlot.TryGet(slot.StringKey, out var need) && other?.Get() is { } incoming && !need.Accepts(incoming);
}
