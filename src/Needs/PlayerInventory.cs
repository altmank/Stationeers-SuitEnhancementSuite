using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace SuitEnhancementSuite;

/// <summary>
/// What one player carries: hands, worn suit, uniform, backpack, toolbelt, helmet, glasses, and containers nested in
/// them. Organ slots are never walked and entities (a carried player or animal) are never searched. The worn suit's
/// need slots are neither sources nor free slots: each is only ever the target of its own need. Hands, and containers
/// held in them, are never a source: what the player holds is in use.
/// Bound to one player at a time and reused; allocation-free, slots are walked by index.
/// </summary>
internal sealed class PlayerInventory
{
    // Human > backpack > box > box > item.
    private const int MaxDepth = 4;

    // Storage before hands: a spent item is stowed in the first free storage slot, and hands are the last resort.
    // Hands are only ever a place to stow, never a source.
    private readonly Slot[] _storage = new Slot[6];
    private readonly Slot[] _hands = new Slot[2];

    public Human Human { get; private set; }

    private DynamicThing Worn { get; set; }

    /// <returns>False when the player wears nothing with slots: there are no need slots to keep.</returns>
    public bool Bind(Human human)
    {
        Human = human;
        Worn = human.SuitSlot?.Get();
        _storage[0] = human.BackpackSlot;
        _storage[1] = human.UniformSlot;
        _storage[2] = human.SuitSlot;
        _storage[3] = human.ToolbeltSlot;
        _storage[4] = human.HelmetSlot;
        _storage[5] = human.GlassesSlot;
        _hands[0] = human.RightHandSlot;
        _hands[1] = human.LeftHandSlot;
        return Worn?.Slots != null;
    }

    public bool TryFindNeedSlot(NeedSlot need, out Slot slot) => SuitSlotLayout.TryFindSlot(Worn.Slots, need.Key, out slot);

    /// <summary>Lets <paramref name="pick"/> weigh every usable item for <paramref name="need"/> outside the hands.</summary>
    public void OfferReplacements(NeedSlot need, ReplacementPick pick)
    {
        pick.Clear();
        Offer(_storage, need, pick);
    }

    /// <summary>
    /// A free slot that admits <paramref name="thing"/>: the top-level storage slots first (backpack, uniform, worn
    /// suit, toolbelt, helmet, glasses), then containers nested in them or held, then an empty hand.
    /// </summary>
    public bool TryFindFreeSlot(DynamicThing thing, out Slot free)
    {
        foreach (var root in _storage)
            if (root?.Get() is { } container && container is not Entity && TryFindDirect(container, thing, out free))
                return true;
        if (TryFindNested(_storage, thing, includeDirect: false, out free) ||
            TryFindNested(_hands, thing, includeDirect: true, out free)) return true;
        foreach (var hand in _hands)
        {
            if (!IsFreeFor(hand, thing)) continue;
            free = hand;
            return true;
        }
        free = null;
        return false;
    }

    /// <summary>
    /// Whether <paramref name="source"/>, once its occupant has left, would admit <paramref name="thing"/>: the checks
    /// of Slot.AllowMove apart from occupancy.
    /// </summary>
    public bool CanTakeBack(Slot source, DynamicThing thing) =>
        source is not null && !source.IsLocked && !IsWornNeedSlot(source) &&
        (source.Type == Slot.Class.None || source.Type == thing.SlotType) &&
        thing is not DraggableThing && thing.CanEnter(source);

    /// <summary>Moves <paramref name="thing"/> into a free inventory slot.</summary>
    /// <returns>False when there is none, or the game refused the move.</returns>
    public bool TryStow(DynamicThing thing) => TryFindFreeSlot(thing, out var free) && Moves.ToSlot(thing, free);

    private void Offer(Slot[] roots, NeedSlot need, ReplacementPick pick)
    {
        foreach (var root in roots)
        {
            var occupant = root?.Get();
            if (occupant is null) continue;
            Consider(occupant, need, pick);
            if (occupant is not Entity) Offer(occupant, need, pick, depth: 1);
        }
    }

    private void Offer(Thing container, NeedSlot need, ReplacementPick pick, int depth)
    {
        var slots = container.Slots;
        if (slots == null) return;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var occupant = slot?.Get();
            if (occupant is null || IsWornNeedSlot(slot)) continue;
            Consider(occupant, need, pick);
            if (depth < MaxDepth && occupant is not Entity) Offer(occupant, need, pick, depth + 1);
        }
    }

    private void Consider(DynamicThing thing, NeedSlot need, ReplacementPick pick)
    {
        if (need.IsUsable(thing)) pick.Consider(thing, Human);
    }

    private bool TryFindNested(Slot[] roots, DynamicThing thing, bool includeDirect, out Slot free)
    {
        foreach (var root in roots)
        {
            if (root?.Get() is not { } container || container is Entity) continue;
            if ((includeDirect && TryFindDirect(container, thing, out free)) || TryFindNested(container, thing, depth: 1, out free))
                return true;
        }
        free = null;
        return false;
    }

    private bool TryFindNested(Thing container, DynamicThing thing, int depth, out Slot free)
    {
        free = null;
        var slots = container.Slots;
        if (slots == null || depth >= MaxDepth) return false;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var occupant = slot?.Get();
            if (occupant is null || occupant is Entity || ReferenceEquals(occupant, thing) || IsWornNeedSlot(slot)) continue;
            if (TryFindDirect(occupant, thing, out free) || TryFindNested(occupant, thing, depth + 1, out free)) return true;
        }
        return false;
    }

    private bool TryFindDirect(Thing container, DynamicThing thing, out Slot free)
    {
        var slots = container.Slots;
        if (slots != null)
            for (var i = 0; i < slots.Count; i++)
            {
                if (!IsFreeFor(slots[i], thing)) continue;
                free = slots[i];
                return true;
            }
        free = null;
        return false;
    }

    // Slot.AllowMove is the vanilla "may go there" check; this mod's patch reduces it to the filter for need slots,
    // so emptiness and the lock are checked here as well.
    private bool IsFreeFor(Slot slot, DynamicThing thing) =>
        slot is not null && slot.IsEmpty() && !slot.IsLocked && !IsWornNeedSlot(slot) && Slot.AllowMove(thing, slot);

    private bool IsWornNeedSlot(Slot slot) => ReferenceEquals(slot.Parent, Worn) && NeedSlot.TryGet(slot.StringKey, out _);
}
