using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Clothing.Suits;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Adds the extra slots to clothing, always after the clothing's own slots and after the slots other mods add when it
/// is created, so no existing slot index ever moves. Saves store each item by slot index, so the order of
/// <see cref="SlotPlan"/> is permanent (tools\slot_layout.json). Adding is idempotent.
/// </summary>
internal static class SuitSlotLayout
{
    /// <summary>Adds the slots when <paramref name="clothing"/> is supported and does not have them yet.</summary>
    /// <returns>True when slots were added.</returns>
    public static bool AddSlots(DynamicThing clothing)
    {
        if (clothing?.Slots is not { } slots || !TryClassify(clothing, out var garment)) return false;
        var plan = SlotPlan.For(garment);
        if (TryFindSlot(slots, plan[0].Key, out _) || !TryGetTemplate(slots, out var template)) return false;
        foreach (var added in plan)
            slots.Add(NewSlot(clothing, template, added));
        return true;
    }

    public static bool TryFindSlot(List<Slot> slots, string key, out Slot slot)
    {
        foreach (var candidate in slots)
        {
            if (candidate == null || candidate.StringKey != key) continue;
            slot = candidate;
            return true;
        }
        slot = null;
        return false;
    }

    /// <summary>
    /// The runtime type decides first: during save deserialization PrefabName is not populated yet. Advanced suits are
    /// the Hardsuit (ItemHardSuit, the game's AdvancedSuit class), the HARM suit, and the spawn-only Hardsuit and
    /// advanced AC suit of the newer suit family.
    /// </summary>
    private static bool TryClassify(DynamicThing thing, out Garment garment)
    {
        switch (thing)
        {
            case Uniform:
                garment = Garment.Uniform;
                return true;
            case BodyArmor:
                garment = Garment.BodyArmor;
                return true;
            case AdvancedSuit or HardSuit or HARMSuit or AdvancedACSuit:
                garment = Garment.AdvancedSuit;
                return true;
            default:
                garment = Garment.Suit;
                return thing is Suit or SuitBase || HasSuitName(thing.PrefabName ?? string.Empty);
        }
    }

    private static bool HasSuitName(string name) =>
        name.IndexOf("Suit", StringComparison.OrdinalIgnoreCase) >= 0 ||
        name.IndexOf("Icarus", StringComparison.OrdinalIgnoreCase) >= 0 ||
        name.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool TryGetTemplate(List<Slot> slots, out Slot template)
    {
        template = slots.Count > 0 ? slots[0] : null;
        return template != null;
    }

    private static Slot NewSlot(DynamicThing parent, Slot template, AddedSlot added) => new()
    {
        IsInteractable = true,
        IsSwappable = template.IsSwappable,
        HidesOccupant = template.HidesOccupant,
        IsHiddenInSeat = template.IsHiddenInSeat,
        OccupantCastsShadows = template.OccupantCastsShadows,
        StringKey = added.Key,
        Parent = parent,
        Type = added.Kind == AddedSlotKind.SharedPower ? Slot.Class.Battery : Slot.Class.None,
        StringHash = Animator.StringToHash(added.Key),
        SlotTypeIcon = SlotIcons.For(added.Kind),
    };
}
