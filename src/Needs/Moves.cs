using Assets.Scripts.Objects;

namespace SuitEnhancementSuite;

/// <summary>The game's own server moves, each checked for having happened: DynamicThing.MoveToSlot can refuse silently.</summary>
internal static class Moves
{
    public static bool ToSlot(DynamicThing thing, Slot slot)
    {
        OnServer.MoveToSlot(thing, slot);
        return ReferenceEquals(thing.ParentSlot, slot);
    }

    /// <summary>
    /// Exchanges the occupants of two slots, as the game's own slot swap does on the server: the first item drops to
    /// the world for the moment the second takes its place, then goes into the second item's former slot. Either
    /// move being refused puts the first item back where it was.
    /// </summary>
    public static bool Swap(DynamicThing first, Slot firstSlot, DynamicThing second, Slot secondSlot)
    {
        first.DamageState.Defend = true;
        second.DamageState.Defend = true;
        try
        {
            OnServer.MoveToWorld(first);
            if (!ToSlot(second, firstSlot))
            {
                ToSlot(first, firstSlot);
                return false;
            }
            if (ToSlot(first, secondSlot)) return true;
            // The second item already sits in the first slot; send it home so the first can return.
            ToSlot(second, secondSlot);
            ToSlot(first, firstSlot);
            return false;
        }
        finally
        {
            first.DamageState.Defend = false;
            second.DamageState.Defend = false;
        }
    }
}
