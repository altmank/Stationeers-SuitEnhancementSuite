using System.Collections.Generic;

namespace SuitEnhancementSuite;

/// <summary>What an added slot holds; it decides the slot class and the empty-slot icon.</summary>
internal enum AddedSlotKind
{
    /// <summary>Takes anything (slot class None, no icon).</summary>
    Storage,
    Water,
    Food,
    Waste,

    /// <summary>A battery cell that charges the others (slot class Battery).</summary>
    SharedPower,
}

/// <summary>The garments that get slots, as far as their slot plan goes.</summary>
internal enum Garment
{
    Uniform,

    /// <summary>The Marine Vest.</summary>
    BodyArmor,

    /// <summary>Any other suit: EVA, emergency, Icarus, insulated, space and normal suits.</summary>
    Suit,

    /// <summary>The Hardsuit and the other top-tier suits: they also carry the Shared Power slots.</summary>
    AdvancedSuit,
}

/// <summary>One slot this mod appends to a garment.</summary>
internal readonly struct AddedSlot(string key, AddedSlotKind kind)
{
    public string Key => key;

    public AddedSlotKind Kind => kind;
}

/// <summary>
/// The slots appended to each garment, in order, behind the garment's own slots and behind any slot another mod adds
/// when the garment is created. Saves store items by slot index, so a plan only ever grows at its end
/// (tools\slot_layout.json).
/// </summary>
internal static class SlotPlan
{
    private static readonly AddedSlot[] NeedSlots =
    [
        new(SlotKeys.Water, AddedSlotKind.Water),
        new(SlotKeys.Food, AddedSlotKind.Food),
        new(SlotKeys.Waste, AddedSlotKind.Waste),
    ];

    private static readonly AddedSlot[] UniformSlots = Storage(4, SlotKeys.UniformStorage);

    private static readonly AddedSlot[] BodyArmorSlots = [.. Storage(8, SlotKeys.ArmorStorage), .. NeedSlots];

    private static readonly AddedSlot[] AdvancedSuitSlots =
    [
        .. NeedSlots,
        new(SlotKeys.FirstSharedPower, AddedSlotKind.SharedPower),
        new(SlotKeys.SecondSharedPower, AddedSlotKind.SharedPower),
    ];

    public static IReadOnlyList<AddedSlot> For(Garment garment) => garment switch
    {
        Garment.Uniform => UniformSlots,
        Garment.BodyArmor => BodyArmorSlots,
        Garment.AdvancedSuit => AdvancedSuitSlots,
        _ => NeedSlots,
    };

    private static AddedSlot[] Storage(int count, System.Func<int, string> key)
    {
        var slots = new AddedSlot[count];
        for (var i = 0; i < count; i++)
            slots[i] = new AddedSlot(key(i + 1), AddedSlotKind.Storage);
        return slots;
    }
}
