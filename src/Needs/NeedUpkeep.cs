using Assets.Scripts.Objects.Entities;

namespace SuitEnhancementSuite;

/// <summary>
/// Per-need state of the automatic pass: the use cooldown, the reusable replacement pick, and which players have been
/// warned that the need is blocked for lack of inventory room (warned once until the need next succeeds).
/// </summary>
internal sealed class NeedUpkeep(NeedSlot need)
{
    private readonly ReportLatch _blocked = new();

    public NeedSlot Need => need;

    public NeedCooldown Cooldown { get; } = new();

    public ReplacementPick Pick { get; } = need.CreatePick();

    /// <param name="step">What was blocked, for the warning: "refilled" or "used".</param>
    public void Record(StepResult result, Human human, string step)
    {
        if (result.IsDone)
            _blocked.Clear(human.ReferenceId);
        else if (result.IsBlocked && _blocked.TryRaise(human.ReferenceId))
            Plugin.Log.LogWarning($"{human.DisplayName}'s {need.SlotName} slot was not {step}: no free inventory slot " +
                                  $"for {result.Unplaced?.PrefabName}. Reported once until the slot next succeeds.");
    }
}
