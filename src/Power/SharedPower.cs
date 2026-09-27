using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;

namespace SuitEnhancementSuite;

/// <summary>
/// Moves energy from the batteries in the worn advanced suit's Shared Power slots into the other batteries a player
/// carries.
/// Runs at the end of every power tick on the game's power thread, the thread that also drains those batteries, on the
/// simulation authority only. It writes <see cref="BatteryCell.PowerStored"/>; the cell's next OnPowerTick recomputes
/// the displayed percentage and sends it to clients, so there is no traffic of its own.
/// Allocation-free while inventories keep their size: buffers are reused and slots are walked by index.
/// </summary>
internal sealed class SharedPower(SharedPowerSettings settings)
{
    // Human > backpack > tool > battery is depth 3; one more level for a tool in a bag in a bag.
    private const int MaxDepth = 4;
    private const int MaxReportedFaults = 16;

    private readonly PowerSharePlan _plan = new();
    private readonly List<BatteryCell> _targets = new(16);
    private readonly List<BatteryCell> _sources = new(2);
    private readonly HashSet<string> _reportedFaults = new();

    /// <summary>One power tick for every living player. Never throws: a failure is logged once per distinct message.</summary>
    public void Tick()
    {
        try
        {
            if (!GameManager.RunSimulation || !settings.Enabled) return;
            var policy = settings.Policy();
            var chargeSuitBattery = settings.ChargeSuitBattery;
            var humans = Human.AllHumans;
            // Counting down tolerates a player leaving on the main thread mid-pass, as the game's own loops do.
            for (var i = humans.Count - 1; i >= 0; i--)
            {
                var human = i < humans.Count ? humans[i] : null;
                if (human is null || human.State != EntityState.Alive) continue;
                try
                {
                    Share(human, policy, chargeSuitBattery);
                }
                catch (Exception e)
                {
                    ReportOnce(e);
                }
            }
        }
        catch (Exception e)
        {
            ReportOnce(e);
        }
    }

    private void Share(Human human, PowerSharingPolicy policy, bool chargeSuitBattery)
    {
        _plan.Clear();
        _sources.Clear();
        _targets.Clear();
        // Only an advanced suit has Shared Power slots (SlotPlan), so any other suit, or none, finds no source.
        if (human.SuitSlot?.Get() is not { Slots: not null } worn || !CollectSources(worn)) return;
        CollectTargets(human, human, BatteryPlace.Elsewhere, worn as ISuit, chargeSuitBattery, depth: 0);
        if (_plan.TargetCount == 0) return;
        _plan.Distribute(policy);
        if (_plan.TotalDrawn <= 0f) return;
        for (var s = 0; s < _sources.Count; s++)
        {
            var drawn = _plan.DrawnFrom(s);
            if (drawn > 0f) _sources[s].PowerStored -= drawn;
        }
        for (var t = 0; t < _targets.Count; t++)
        {
            var delivered = _plan.DeliveredTo(t);
            if (delivered > 0f) _targets[t].PowerStored += delivered;
        }
    }

    /// <returns>True when at least one Shared Power cell holds energy.</returns>
    private bool CollectSources(DynamicThing suit)
    {
        var slots = suit.Slots;
        var anyCharge = false;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot is null || !SlotKeys.IsSharedPower(slot.StringKey) || slot.Get() is not BatteryCell cell) continue;
            _sources.Add(cell);
            _plan.AddSource(cell.PowerStored);
            anyCharge |= cell.PowerStored > 0f;
        }
        return anyCharge;
    }

    private void CollectTargets(Thing parent, Human human, BatteryPlace branch, ISuit wornSuit, bool chargeSuitBattery, int depth)
    {
        var slots = parent.Slots;
        if (slots == null) return;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var occupant = slot?.Get();
            if (occupant is null) continue;
            var place = depth == 0 && (slot == human.LeftHandSlot || slot == human.RightHandSlot) ? BatteryPlace.Hand : branch;
            if (slot.Type == Slot.Class.Battery && occupant is BatteryCell cell)
                AddTarget(cell, PlaceOf(slot, parent, wornSuit, place), chargeSuitBattery);
            else if (depth < MaxDepth)
                CollectTargets(occupant, human, place, wornSuit, chargeSuitBattery, depth + 1);
        }
    }

    // ISuit covers both suit families: the Hardsuit is the older Suit class, whose battery slot SuitBase never sees.
    private static BatteryPlace PlaceOf(Slot slot, Thing parent, ISuit wornSuit, BatteryPlace branch)
    {
        if (SlotKeys.IsSharedPower(slot.StringKey)) return BatteryPlace.SharedPower;
        if (wornSuit is not null && ReferenceEquals(parent, wornSuit.AsThing) && ReferenceEquals(wornSuit.BatterySlot, slot))
            return BatteryPlace.SuitBattery;
        return branch;
    }

    private void AddTarget(BatteryCell cell, BatteryPlace place, bool chargeSuitBattery)
    {
        if (!place.TryGetPriority(chargeSuitBattery, out var priority)) return;
        _targets.Add(cell);
        _plan.AddTarget(priority, new Charge(cell.PowerStored, cell.PowerMaximum));
    }

    private void ReportOnce(Exception e)
    {
        var key = e.GetType().Name + ": " + e.Message;
        if (_reportedFaults.Count >= MaxReportedFaults || !_reportedFaults.Add(key)) return;
        Plugin.Log.LogWarning($"Shared power skipped a player or tick (reported once): {e}");
    }
}
