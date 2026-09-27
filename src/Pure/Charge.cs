using System;

namespace SuitEnhancementSuite;

/// <summary>Energy in a battery against its capacity, in the game's joules.</summary>
internal readonly struct Charge(float stored, float capacity)
{
    public float Stored => stored;

    public float Capacity => capacity;

    public float Room => Math.Max(0f, capacity - stored);

    /// <summary>Stored share of capacity; a battery without capacity reads full so it is served last.</summary>
    public float Ratio => capacity > 0f ? stored / capacity : 1f;
}
