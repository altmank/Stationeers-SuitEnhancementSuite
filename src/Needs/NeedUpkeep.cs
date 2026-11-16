using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace SuitEnhancementSuite;

/// <summary>
/// Per-need state of the automatic pass: the use cooldown, the wait for the game to apply the last use, the reusable
/// replacement pick, and which players have been warned that the need is blocked for lack of inventory room (warned
/// once until the need next succeeds).
/// </summary>
internal sealed class NeedUpkeep(NeedSlot need)
{
    private readonly ReportLatch _blocked = new();
    private string _lastStep;
    private DynamicThing _lastUnplaced;

    public NeedSlot Need => need;

    public NeedCooldown Cooldown { get; } = new();

    public TickGate Settle { get; } = new();

    public ReplacementPick Pick { get; } = need.CreatePick();

    /// <param name="step">What was blocked, for the warning: "refilled" or "used".</param>
    public void Record(StepResult result, Human human, string step)
    {
        if (result.IsDone)
            _blocked.Clear(human.ReferenceId);
        else if (result.IsBlocked)
        {
            _lastStep = step;
            _lastUnplaced = result.Unplaced;
            if (_blocked.TryRaise(human.ReferenceId))
                Plugin.Log.LogWarning($"{human.DisplayName}'s {need.SlotName} slot was not {step}: no free inventory slot " +
                                      $"for {LogNames.Of(result.Unplaced)} ({LogNames.Of(human)}). Reported once until the slot " +
                                      $"next succeeds; it retries every {AutoConsumer.PassSeconds} s.");
        }
    }

    /// <summary>The last time this need was blocked, for <c>ses diag</c>; null until then.</summary>
    public string LastRefusal => _lastStep == null
        ? null
        : $"{need.SlotName} slot not {_lastStep}: no free inventory slot for {LogNames.Of(_lastUnplaced)}";
}
