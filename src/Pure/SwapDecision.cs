namespace SuitEnhancementSuite;

/// <summary>What a need slot holds, as far as refilling it goes.</summary>
internal enum SlotContent
{
    Empty,
    Usable,
    /// <summary>An item the slot can no longer serve from: an empty bottle, rotten food, a full waste bag.</summary>
    Spent,
}

/// <summary>How the chosen replacement enters the slot.</summary>
internal enum ReplacementShape
{
    /// <summary>The item moves in whole and leaves its source slot empty.</summary>
    Whole,

    /// <summary>One unit is split off a stack into the slot; the rest of the stack stays in its source slot.</summary>
    OneOfStack,
}

/// <summary>The legal ways to refill a need slot. Every plan uses only the game's own moves.</summary>
internal enum SwapPlan
{
    Keep,
    MoveIn,
    SplitIn,

    /// <summary>The spent item takes the replacement's place: a true swap.</summary>
    SwapBack,

    /// <summary>The spent item goes to a free inventory slot, then the replacement moves in.</summary>
    StowThenMoveIn,

    /// <summary>The spent item goes to a free inventory slot, then one unit is split into the slot.</summary>
    StowThenSplitIn,

    /// <summary>The spent item has nowhere to go: nothing moves.</summary>
    Blocked,
}

/// <summary>Picks how a need slot is refilled once a replacement has been found.</summary>
internal static class SwapDecision
{
    /// <param name="sourceTakesSpent">The replacement's source slot, once empty, would admit the spent item.</param>
    /// <param name="hasFreeSlot">Some free inventory slot admits the spent item.</param>
    public static SwapPlan Decide(SlotContent content, ReplacementShape replacement, bool sourceTakesSpent, bool hasFreeSlot) =>
        content switch
        {
            SlotContent.Empty => replacement == ReplacementShape.Whole ? SwapPlan.MoveIn : SwapPlan.SplitIn,
            SlotContent.Spent => ForSpent(replacement, sourceTakesSpent, hasFreeSlot),
            _ => SwapPlan.Keep,
        };

    private static SwapPlan ForSpent(ReplacementShape replacement, bool sourceTakesSpent, bool hasFreeSlot)
    {
        // A split leaves the rest of the stack in its source slot, so only a whole move can swap back.
        if (replacement == ReplacementShape.Whole && sourceTakesSpent) return SwapPlan.SwapBack;
        if (!hasFreeSlot) return SwapPlan.Blocked;
        return replacement == ReplacementShape.Whole ? SwapPlan.StowThenMoveIn : SwapPlan.StowThenSplitIn;
    }
}
