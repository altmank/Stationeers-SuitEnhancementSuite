namespace SuitEnhancementSuite;

/// <summary>What one inventory item would give a need slot, reduced to what ranks it against the others.</summary>
internal interface IOffer<T> where T : struct, IOffer<T>
{
    /// <summary>True when this offer is strictly better. An equal offer keeps the incumbent, so ties go to the first found.</summary>
    bool Beats(T incumbent);
}
