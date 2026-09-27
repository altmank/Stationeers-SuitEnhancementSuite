using System;
using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Feeds and waters every living player from the Water and Food slots of the suit they wear. Runs on the simulation
/// authority only (host or dedicated server); the game syncs the resulting nutrition, hydration and item quantities.
/// </summary>
internal sealed class AutoConsumer(AutoConsumeSettings settings)
{
    public const float PassSeconds = 2f;

    private const int PrunePasses = 30;
    private const int MaxReportedFaults = 16;

    private readonly NeedCooldown _waterCooldown = new();
    private readonly NeedCooldown _foodCooldown = new();
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
                    Serve(human, policy, now, cooldown);
                }
                catch (Exception e)
                {
                    ReportOnce(e);
                }
            }
            if (++_passes % PrunePasses != 0) return;
            _waterCooldown.Prune(now);
            _foodCooldown.Prune(now);
        }
        catch (Exception e)
        {
            ReportOnce(e);
        }
    }

    private void Serve(Human human, ConsumptionPolicy policy, float now, float cooldown)
    {
        var worn = human.SuitSlot?.Get();
        if (worn == null || worn.Slots == null) return;
        Serve(NeedSlot.Water, _waterCooldown, worn, human, policy, now, cooldown);
        Serve(NeedSlot.Food, _foodCooldown, worn, human, policy, now, cooldown);
    }

    private static void Serve(NeedSlot need, NeedCooldown cooldown, DynamicThing worn, Human human,
        ConsumptionPolicy policy, float now, float cooldownSeconds)
    {
        if (!cooldown.IsReady(human.ReferenceId, now)) return;
        if (!SuitSlotLayout.TryFindSlot(worn.Slots, need.Key, out var slot)) return;
        var item = slot.Get();
        if (item == null || !need.Accepts(item)) return;
        var prefab = item.PrefabName;
        var before = need.Level(human);
        if (!need.TryServe(item, human, policy, out var amount)) return;
        cooldown.Start(human.ReferenceId, now, cooldownSeconds);
        Plugin.Log.LogDebug(string.Format(CultureInfo.InvariantCulture,
            "Auto consume: {0} used {1:0.###} of {2}, {3} {4:0.##} -> {5:0.##}",
            human.DisplayName, amount, prefab, need.NeedName, before, need.Level(human)));
    }

    private void ReportOnce(Exception e)
    {
        var key = e.GetType().Name + ": " + e.Message;
        if (_reportedFaults.Count >= MaxReportedFaults || !_reportedFaults.Add(key)) return;
        Plugin.Log.LogWarning($"Auto consume skipped a player or pass (reported once): {e}");
    }
}
