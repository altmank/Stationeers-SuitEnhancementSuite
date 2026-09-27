using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Adds the extra slots to clothing, always after the clothing's own slots, so the game's slot indices never move.
/// Saves store each item by slot index, so this order is permanent (tools\slot_layout.json):
/// suits get Water and Food; body armor gets eight storage slots, then Water and Food; a uniform gets four storage
/// slots, then two Shared Power battery slots. Adding is idempotent.
/// </summary>
internal static class SuitSlotLayout
{
    private const int ArmorStorageCount = 8;
    private const int UniformStorageCount = 4;

    /// <summary>Adds the slots when <paramref name="clothing"/> is supported and does not have them yet.</summary>
    /// <returns>True when slots were added.</returns>
    public static bool AddSlots(DynamicThing clothing)
    {
        if (clothing == null || !IsSupported(clothing) || clothing.Slots == null) return false;
        return clothing is Uniform ? AddUniformSlots(clothing) : AddSuitSlots(clothing);
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

    private static bool AddUniformSlots(DynamicThing uniform)
    {
        var slots = uniform.Slots;
        if (TryFindSlot(slots, SlotKeys.FirstUniformStorage, out _) || !TryGetTemplate(slots, out var template)) return false;
        for (var number = 1; number <= UniformStorageCount; number++)
            slots.Add(NewSlot(uniform, template, SlotKeys.UniformStorage(number), Slot.Class.None, icon: null));
        slots.Add(NewSlot(uniform, template, SlotKeys.FirstSharedPower, Slot.Class.Battery, SlotIcons.SharedPower));
        slots.Add(NewSlot(uniform, template, SlotKeys.SecondSharedPower, Slot.Class.Battery, SlotIcons.SharedPower));
        return true;
    }

    private static bool AddSuitSlots(DynamicThing suit)
    {
        var slots = suit.Slots;
        if (TryFindSlot(slots, SlotKeys.Water, out _) || !TryGetTemplate(slots, out var template)) return false;
        if (suit is BodyArmor)
            for (var number = 1; number <= ArmorStorageCount; number++)
                slots.Add(NewSlot(suit, template, SlotKeys.ArmorStorage(number), Slot.Class.None, icon: null));
        slots.Add(NewSlot(suit, template, SlotKeys.Water, Slot.Class.None, SlotIcons.Water));
        slots.Add(NewSlot(suit, template, SlotKeys.Food, Slot.Class.None, SlotIcons.Food));
        return true;
    }

    private static bool TryGetTemplate(List<Slot> slots, out Slot template)
    {
        template = slots.Count > 0 ? slots[0] : null;
        return template != null;
    }

    private static Slot NewSlot(DynamicThing parent, Slot template, string key, Slot.Class type, Sprite icon) => new()
    {
        IsInteractable = true,
        IsSwappable = template.IsSwappable,
        HidesOccupant = template.HidesOccupant,
        IsHiddenInSeat = template.IsHiddenInSeat,
        OccupantCastsShadows = template.OccupantCastsShadows,
        StringKey = key,
        Parent = parent,
        Type = type,
        StringHash = Animator.StringToHash(key),
        SlotTypeIcon = icon,
    };

    private static bool IsSupported(DynamicThing thing)
    {
        // During save deserialization PrefabName is not populated yet, so the runtime type decides first.
        if (thing is Uniform || thing is SuitBase || thing is BodyArmor) return true;
        var name = thing.PrefabName ?? string.Empty;
        return name.IndexOf("Suit", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Icarus", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Armor", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name == "ItemEvaSuit";
    }
}
