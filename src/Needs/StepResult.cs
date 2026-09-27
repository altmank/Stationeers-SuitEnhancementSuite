using Assets.Scripts.Objects;

namespace SuitEnhancementSuite;

/// <summary>How one step (refill or use) on one need slot went.</summary>
internal readonly struct StepResult
{
    private readonly Outcome _outcome;

    private StepResult(Outcome outcome, DynamicThing unplaced)
    {
        _outcome = outcome;
        Unplaced = unplaced;
    }

    private enum Outcome
    {
        Idle,
        Done,
        Blocked,
    }

    public static StepResult Idle => new(Outcome.Idle, null);

    public static StepResult Done => new(Outcome.Done, null);

    public bool IsDone => _outcome == Outcome.Done;

    public bool IsBlocked => _outcome == Outcome.Blocked;

    /// <summary>The item that found no free inventory slot; set only when blocked.</summary>
    public DynamicThing Unplaced { get; }

    public static StepResult NoRoomFor(DynamicThing unplaced) => new(Outcome.Blocked, unplaced);

    public static StepResult From(bool done) => done ? Done : Idle;
}
