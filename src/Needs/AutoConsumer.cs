using System;
using System.Collections.Generic;
using System.Globalization;
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
                    ReportOnce(e);
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
            ReportOnce(e);
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
        var before = need.Level(human);
        var result = need.Serve(slot, _player, policy, out var amount);
        if (!result.IsDone) return result;
        upkeep.Cooldown.Start(human.ReferenceId, now, cooldownSeconds);
        upkeep.Settle.Close(human.ReferenceId, tick);
        Plugin.Log.LogDebug(string.Format(CultureInfo.InvariantCulture,
            "Auto consume: {0} used {1:0.###} of {2}, {3} {4:0.##} -> {5:0.##}",
            human.DisplayName, amount, prefab, need.NeedName, before, need.Level(human)));
        return result;
    }

    private void ReportOnce(Exception e)
    {
        var key = e.GetType().Name + ": " + e.Message;
        if (_reportedFaults.Count >= MaxReportedFaults || !_reportedFaults.Add(key)) return;
        Plugin.Log.LogWarning($"Auto consume skipped a player or pass (reported once): {e}");
    }
}
