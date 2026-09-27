using System.Collections.Generic;

namespace SuitEnhancementSuite;

/// <summary>Earliest time, per player reference id, at which one need may be served again. Times are real seconds.</summary>
internal sealed class NeedCooldown
{
    private readonly Dictionary<long, float> _readyAt = new();
    private readonly List<long> _expired = new();

    public int Count => _readyAt.Count;

    public bool IsReady(long playerId, float now) => !_readyAt.TryGetValue(playerId, out var readyAt) || now >= readyAt;

    public void Start(long playerId, float now, float seconds) => _readyAt[playerId] = now + seconds;

    /// <summary>Drops every entry that has run out, including those of players who left.</summary>
    public void Prune(float now)
    {
        _expired.Clear();
        foreach (var entry in _readyAt)
            if (now >= entry.Value)
                _expired.Add(entry.Key);
        foreach (var playerId in _expired)
            _readyAt.Remove(playerId);
    }
}
