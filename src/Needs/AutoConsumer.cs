using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Feeds, waters and relieves every living player from the Water, Food and Waste slots of the suit they wear, and
/// refills those slots from the player's inventory. Runs on the simulation authority only (host or dedicated server),
/// on the main thread; the game syncs the resulting needs, item quantities and moves.
/// </summary>
internal sealed class AutoConsumer(AutoConsumeSettings settings, AutoSwapSettings swapSettings)
{
    public const float PassSeconds = 2f;

    private const int PrunePasses = 30;
    private const int MaxReportedFaults = 16;

    private readonly NeedUpkeep[] _upkeeps = [new(NeedSlot.Water), new(NeedSlot.Food), new(NeedSlot.Waste)];
    private readonly PlayerInventory _player = new();
    private readonly HashSet<string> _reportedFaults = new();
    private int _passes;
    private bool _capReported;
    private string _lastFault;

    /// <summary>One pass over all players. Never throws: a failure is logged once per distinct message.</summary>
    public void RunPass()
    {
        try
        {
            if (!settings.Enabled || !GameManager.RunSimulation) return;
            var policy = settings.Policy();
            var cooldown = settings.CooldownSeconds;
            var now = Time.unscaledTime;
            foreach (var human in Human.AllHumans)
            {
                if (human == null || human.State != EntityState.Alive) continue;
                try
                {
                    Upkeep(human, policy, now, cooldown);
                }
                catch (Exception e)
                {
                    ReportOnce(e, LogNames.Of(human));
                }
            }
            if (++_passes % PrunePasses != 0) return;
            var tick = GameManager.GameTickCount;
            foreach (var upkeep in _upkeeps)
            {
                upkeep.Cooldown.Prune(now);
                upkeep.Settle.Prune(tick);
            }
        }
        catch (Exception e)
        {
            ReportOnce(e, "every player");
        }
    }

    /// <summary>Per need: refill first, so a freshly swapped-in item can be used in the same pass.</summary>
    private void Upkeep(Human human, ConsumptionPolicy policy, float now, float cooldown)
    {
        if (!_player.Bind(human)) return;
        foreach (var upkeep in _upkeeps)
        {
            if (!_player.TryFindNeedSlot(upkeep.Need, out var slot)) continue;
            if (swapSettings.IsOn(upkeep.Need))
                upkeep.Record(AutoSwapper.Refill(upkeep.Need, slot, _player, upkeep.Pick), human, "refilled");
            upkeep.Record(Serve(upkeep, slot, policy, now, cooldown), human, "used");
        }
    }

    /// <summary>
    /// Uses the slot when the need is due, the cooldown has run out and the game has applied the last use. This pass
    /// runs on real time and so also while the game is paused; without the tick gate a waste bag would be filled again
    /// from a stomach the game has not drained yet, creating waste and using up every bag the player carries.
    /// </summary>
    private StepResult Serve(NeedUpkeep upkeep, Slot slot, ConsumptionPolicy policy, float now, float cooldownSeconds)
    {
        var human = _player.Human;
        var need = upkeep.Need;
        var tick = GameManager.GameTickCount;
        if (!upkeep.Cooldown.IsReady(human.ReferenceId, now) || !upkeep.Settle.IsOpen(human.ReferenceId, tick))
            return StepResult.Idle;
        var item = slot.Get();
        if (item == null || !need.Accepts(item)) return StepResult.Idle;
        var prefab = item.PrefabName;
        var itemId = item.ReferenceId;
        var before = need.Level(human);
        var result = need.Serve(slot, _player, policy, out var amount);
        if (!result.IsDone) return result;
        upkeep.Cooldown.Start(human.ReferenceId, now, cooldownSeconds);
        upkeep.Settle.Close(human.ReferenceId, tick);
        if (Plugin.Verbose.On(LogArea.Consume)) LogUse(human, amount, prefab, itemId, need, before);
        return result;
    }

    private static void LogUse(Human human, float amount, string prefab, long itemId, NeedSlot need, float before) =>
        Plugin.Log.LogInfo(string.Format(CultureInfo.InvariantCulture,
            "Auto consume: {0} used {1:0.###} of {2}, {3} {4:0.##} -> {5:0.##} (player {6}, item {7})",
            human.DisplayName, amount, prefab, need.NeedName, before, need.Level(human),
            LogNames.Of(human), LogText.Thing(prefab, itemId)));

    private void ReportOnce(Exception e, string who)
    {
        var key = e.GetType().Name + ": " + e.Message;
        if (_reportedFaults.Count >= MaxReportedFaults)
        {
            if (_capReported) return;
            _capReported = true;
            Plugin.Log.LogWarning($"Auto consume has reported {MaxReportedFaults} different problems; new ones are no longer logged.");
            return;
        }
        _lastFault = key;
        if (!_reportedFaults.Add(key)) return;
        Plugin.Log.LogWarning($"Auto consume skipped {who} this pass and retries in {PassSeconds} s (reported once): {e}");
    }

    /// <summary>Per living player and need slot: the item, the waits, and the last refusal, for <c>ses diag</c>.</summary>
    internal void Describe(StringBuilder text)
    {
        var now = Time.unscaledTime;
        var tick = GameManager.GameTickCount;
        var seen = 0;
        foreach (var human in Human.AllHumans)
        {
            if (human == null || human.State != EntityState.Alive) continue;
            seen++;
            text.Append("\n    ").Append(LogNames.Of(human)).Append(' ').Append(human.DisplayName);
            if (!_player.Bind(human))
            {
                text.Append(": wears nothing with slots");
                continue;
            }
            foreach (var upkeep in _upkeeps)
            {
                text.Append("\n      ").Append(upkeep.Need.SlotName).Append(": ");
                if (!_player.TryFindNeedSlot(upkeep.Need, out var slot)) text.Append("no slot on the worn suit");
                else
                    text.Append(slot.Get() is { } item ? LogNames.Of(item) : "empty")
                        .Append(", cooldown ").Append(upkeep.Cooldown.IsReady(human.ReferenceId, now) ? "ready" : "running")
                        .Append(", game tick ").Append(upkeep.Settle.IsOpen(human.ReferenceId, tick) ? "settled" : "waiting")
                        .Append(", last refusal ").Append(upkeep.LastRefusal ?? "none");
            }
        }
        if (seen == 0) text.Append("no living player");
        text.Append("\n    last fault: ").Append(_lastFault ?? "none");
    }
}
