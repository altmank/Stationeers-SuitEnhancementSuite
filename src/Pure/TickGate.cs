using System.Collections.Generic;

namespace SuitEnhancementSuite;

/// <summary>
/// Per player, the game tick at which one need was last served. The need is served again only after the game has
/// completed <see cref="SettleTicks"/> more ticks, so the game has applied the last use: a waste bag drains the
/// stomach through an atmospheric event that the next game tick applies, and until then the stomach still reads full.
/// No tick completes while the game is paused.
/// </summary>
internal sealed class TickGate
{
    /// <summary>
    /// A use made while tick N runs is applied at the start of tick N + 1, which is complete once the counter has moved
    /// on twice.
    /// </summary>
    public const uint SettleTicks = 2;

    private readonly Dictionary<long, uint> _closedAt = new();
    private readonly List<long> _settled = new();

    public int Count => _closedAt.Count;

    public bool IsOpen(long playerId, uint tick) =>
        !_closedAt.TryGetValue(playerId, out var closedAt) || HasSettled(closedAt, tick);

    public void Close(long playerId, uint tick) => _closedAt[playerId] = tick;

    /// <summary>Drops every entry that has settled, including those of players who left.</summary>
    public void Prune(uint tick)
    {
        _settled.Clear();
        foreach (var entry in _closedAt)
            if (HasSettled(entry.Value, tick))
                _settled.Add(entry.Key);
        foreach (var playerId in _settled)
            _closedAt.Remove(playerId);
    }

    // Unsigned difference: a counter that restarted with a new world, or wrapped, reads as settled.
    private static bool HasSettled(uint closedAt, uint tick) => unchecked(tick - closedAt) >= SettleTicks;
}
