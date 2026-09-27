using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using HarmonyLib;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Slot names come from the private Localization.SlotsName table (keyed by the slot's StringHash). Registering the
/// labels there shows them instead of an &lt;NC:...&gt; placeholder.
/// </summary>
[HarmonyPatch(typeof(Localization.Language), "Load")]
internal static class SlotNamesLocalizationPatch
{
    private static void Postfix()
    {
        try
        {
            if (AccessTools.Field(typeof(Localization), "SlotsName")?.GetValue(null) is not Dictionary<int, string> names) return;
            foreach (var key in LabelledSlots.Keys)
                if (LabelledSlots.TryGet(key, out var label, out _))
                    names[Animator.StringToHash(key)] = label;
        }
        catch (Exception e) { Plugin.Log?.LogDebug($"Slot labels unavailable: {e.Message}"); }
    }
}

/// <summary>Slot.Initialize resets the icon from the slot class; the labelled slots keep theirs.</summary>
[HarmonyPatch(typeof(Slot), "Initialize")]
internal static class SlotInitializeIconPatch
{
    private static void Postfix(Slot __instance)
    {
        if (__instance != null && LabelledSlots.TryGet(__instance.StringKey, out _, out var icon) && icon != null)
            __instance.SlotTypeIcon = icon;
    }
}

[HarmonyPatch(typeof(InventoryWindow), "SetSlots")]
internal static class InventoryWindowSetSlotsLabelPatch
{
    private static void Postfix(InventoryWindow __instance)
    {
        var slots = __instance?.Parent?.Slots;
        if (slots == null) return;
        foreach (var slot in slots)
            SlotLabels.Apply(slot);
    }
}

/// <summary>Vanilla clears the label of every slot of class None on refresh.</summary>
[HarmonyPatch(typeof(Slot), "RefreshSlotDisplay")]
internal static class SlotRefreshLabelPatch
{
    private static void Postfix(Slot __instance) => SlotLabels.Apply(__instance);
}

internal static class SlotLabels
{
    private static readonly Color EmptyIconTint = new(1f, 1f, 1f, .07f);

    /// <summary>Shows the label and, while the slot is empty, the icon of a labelled slot. Other slots are left alone.</summary>
    public static void Apply(Slot slot)
    {
        if (slot == null || !LabelledSlots.TryGet(slot.StringKey, out var label, out var icon)) return;
        var button = slot.Button;
        if (icon != null)
        {
            // Building the button can restore the template's icon (for example a cartridge).
            slot.SlotTypeIcon = icon;
            // Vanilla has drawn the display already; only the empty-slot artwork is replaced, never an occupant's icon.
            if (button?.Image != null && slot.Get() == null)
            {
                button.Image.sprite = icon;
                button.Image.color = EmptyIconTint;
            }
        }
        if (button?.Text != null)
            button.Text.text = label;
    }
}
