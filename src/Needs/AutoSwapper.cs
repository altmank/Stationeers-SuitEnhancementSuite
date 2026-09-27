using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace SuitEnhancementSuite;

/// <summary>
/// Refills an empty need slot, or one whose item is spent, with the best usable item the player carries. Only the
/// game's own moves are used; a spent item goes where the replacement came from or to a free inventory slot, and when
/// it has nowhere to go nothing moves. Host only.
/// </summary>
internal static class AutoSwapper
{
    public static StepResult Refill(NeedSlot need, Slot target, PlayerInventory player, ReplacementPick pick)
    {
        var spent = target.Get();
        var content = need.Classify(spent);
        if (content == SlotContent.Usable) return StepResult.Idle;
        player.OfferReplacements(need, pick);
        var replacement = pick.Best;
        if (replacement is null) return StepResult.Idle;
        var shape = need.ShapeOf(replacement);
        var source = replacement.ParentSlot;
        Slot free = null;
        var isSpent = content == SlotContent.Spent;
        var plan = SwapDecision.Decide(content, shape,
            sourceTakesSpent: isSpent && player.CanTakeBack(source, spent),
            hasFreeSlot: isSpent && player.TryFindFreeSlot(spent, out free));
        var spentName = isSpent ? spent.PrefabName : null;
        var done = plan switch
        {
            SwapPlan.MoveIn => Moves.ToSlot(replacement, target),
            SwapPlan.SplitIn => SplitIn(replacement, target),
            SwapPlan.SwapBack => SwapBack(spent, target, replacement, source, player),
            SwapPlan.StowThenMoveIn => Moves.ToSlot(spent, free) && Moves.ToSlot(replacement, target),
            SwapPlan.StowThenSplitIn => Moves.ToSlot(spent, free) && SplitIn(replacement, target),
            _ => false,
        };
        if (plan == SwapPlan.Blocked) return StepResult.NoRoomFor(spent);
        if (done) Report(need, player, spentName, target.Get(), source);
        return StepResult.From(done);
    }

    private static bool SplitIn(DynamicThing stack, Slot target)
    {
        var split = ((Stackable)stack).SplitStack(1, target);
        return split != null && ReferenceEquals(split.ParentSlot, target);
    }

    private static bool SwapBack(DynamicThing spent, Slot target, DynamicThing replacement, Slot source, PlayerInventory player)
    {
        if (Moves.Swap(spent, target, replacement, source)) return true;
        // A refused move can leave the spent item in the world; it goes back into the inventory if there is room.
        if (spent.ParentSlot == null && !player.TryStow(spent))
            Plugin.Log.LogWarning($"Auto swap: {spent.PrefabName} of {player.Human.DisplayName} found no slot and lies at their feet.");
        return false;
    }

    private static void Report(NeedSlot need, PlayerInventory player, string spentName, DynamicThing now, Slot source) =>
        Plugin.Log.LogInfo($"Auto swap: {player.Human.DisplayName}'s {need.SlotName} slot " +
                           $"{(spentName == null ? "was empty" : "held spent " + spentName)}, now holds {now?.PrefabName} " +
                           $"from {source?.Parent?.PrefabName ?? "?"}.");
}
