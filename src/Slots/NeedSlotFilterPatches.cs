using Assets.Scripts.Objects;
using HarmonyLib;
using GameStrings = Assets.Scripts.Localization2.GameStrings;

namespace SuitEnhancementSuite;

// The Water, Food and Waste slots are of class None, which vanilla lets anything enter. These three gates (the inventory UI
// checks IsAllowedType and AllowMove before CanEnter) admit exactly what NeedSlot.Accepts admits. Items already in a
// slot are never evicted: a save restores them by index without these checks, and the player can take them out.

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
        if (need.Accepts(__instance))
            __result = CanEnterResult.Succeed;
        else if (__result)
            __result = CanEnterResult.Fail(GameStrings.ThingIsNotType, __instance.DisplayName, destinationSlot.DisplayName);
    }
}
