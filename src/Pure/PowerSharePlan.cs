using System;

namespace SuitEnhancementSuite;

/// <summary>
/// One power tick's transfers from the Shared Power cells (sources) into the other batteries (targets). Reused tick
/// after tick: <see cref="Clear"/>, add targets and sources, <see cref="Distribute"/>, read the amounts. The buffers
/// only grow, so a steady inventory allocates nothing.
/// Order: targets by priority, then the lowest charge ratio; sources emptiest (fewest joules) first, so one cell runs
/// flat and can be swapped while the other stays full.
/// </summary>
internal sealed class PowerSharePlan
{
    private const int InitialCapacity = 16;

    private ChargePriority[] _priorities = new ChargePriority[InitialCapacity];
    private Charge[] _targets = new Charge[InitialCapacity];
    private float[] _delivered = new float[InitialCapacity];
    private int[] _targetOrder = new int[InitialCapacity];
    private float[] _sources = new float[InitialCapacity];
    private float[] _drawn = new float[InitialCapacity];
    private int[] _sourceOrder = new int[InitialCapacity];

    public int TargetCount { get; private set; }

    public int SourceCount { get; private set; }

    public float TotalDelivered { get; private set; }

    public float TotalDrawn { get; private set; }

    public void Clear()
    {
        TargetCount = 0;
        SourceCount = 0;
        TotalDelivered = 0f;
        TotalDrawn = 0f;
    }

    /// <returns>The target's index for <see cref="DeliveredTo"/>.</returns>
    public int AddTarget(ChargePriority priority, Charge charge)
    {
        if (TargetCount == _targets.Length)
        {
            var size = TargetCount * 2;
            Array.Resize(ref _priorities, size);
            Array.Resize(ref _targets, size);
            Array.Resize(ref _delivered, size);
            Array.Resize(ref _targetOrder, size);
        }
        _priorities[TargetCount] = priority;
        _targets[TargetCount] = charge;
        _delivered[TargetCount] = 0f;
        return TargetCount++;
    }

    /// <param name="stored">Joules in the source cell; negative or NaN reads as empty.</param>
    /// <returns>The source's index for <see cref="DrawnFrom"/>.</returns>
    public int AddSource(float stored)
    {
        if (SourceCount == _sources.Length)
        {
            var size = SourceCount * 2;
            Array.Resize(ref _sources, size);
            Array.Resize(ref _drawn, size);
            Array.Resize(ref _sourceOrder, size);
        }
        _sources[SourceCount] = stored > 0f ? stored : 0f;
        _drawn[SourceCount] = 0f;
        return SourceCount++;
    }

    public float DeliveredTo(int target) => _delivered[target];

    public float DrawnFrom(int source) => _drawn[source];

    /// <summary>
    /// Fills each target in order with up to the policy's rate, never past its room, drawing rate / efficiency from
    /// the sources in order. Delivered energy is always drawn energy times efficiency, so nothing is created.
    /// </summary>
    public void Distribute(PowerSharingPolicy policy)
    {
        TotalDelivered = 0f;
        TotalDrawn = 0f;
        var efficiency = policy.Efficiency;
        if (TargetCount == 0 || SourceCount == 0 || efficiency <= 0f || policy.RatePerTarget <= 0f) return;
        SortTargets();
        SortSources();
        var nextSource = 0;
        for (var t = 0; t < TargetCount && nextSource < SourceCount; t++)
        {
            var target = _targetOrder[t];
            var wanted = Math.Min(policy.RatePerTarget, _targets[target].Room);
            if (wanted <= 0f) continue;
            var toDraw = wanted / efficiency;
            while (toDraw > 0f && nextSource < SourceCount)
            {
                var source = _sourceOrder[nextSource];
                var take = Math.Min(_sources[source] - _drawn[source], toDraw);
                _drawn[source] += take;
                toDraw -= take;
                if (_sources[source] - _drawn[source] <= 0f) nextSource++;
                var arrived = take * efficiency;
                _delivered[target] += arrived;
                TotalDrawn += take;
                TotalDelivered += arrived;
            }
        }
    }

    // Insertion sorts: the lists hold a handful of batteries, and sorting in place allocates nothing.
    private void SortTargets()
    {
        for (var i = 0; i < TargetCount; i++)
        {
            var j = i;
            while (j > 0 && ServesBefore(i, _targetOrder[j - 1]))
            {
                _targetOrder[j] = _targetOrder[j - 1];
                j--;
            }
            _targetOrder[j] = i;
        }
    }

    private bool ServesBefore(int a, int b) =>
        _priorities[a] != _priorities[b] ? _priorities[a] < _priorities[b] : _targets[a].Ratio < _targets[b].Ratio;

    private void SortSources()
    {
        for (var i = 0; i < SourceCount; i++)
        {
            var j = i;
            while (j > 0 && _sources[i] < _sources[_sourceOrder[j - 1]])
            {
                _sourceOrder[j] = _sourceOrder[j - 1];
                j--;
            }
            _sourceOrder[j] = i;
        }
    }
}
