using System.Collections.Generic;

namespace SuitEnhancementSuite;

/// <summary>Which players have been told about a condition, so it is reported once per player until it clears.</summary>
internal sealed class ReportLatch
{
    private readonly HashSet<long> _raised = new();

    /// <returns>True the first time for <paramref name="playerId"/> since the last <see cref="Clear"/>.</returns>
    public bool TryRaise(long playerId) => _raised.Add(playerId);

    public void Clear(long playerId) => _raised.Remove(playerId);
}
