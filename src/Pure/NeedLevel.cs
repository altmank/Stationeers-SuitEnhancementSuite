namespace SuitEnhancementSuite;

/// <summary>A body need (nutrition or hydration) as its current level against its current capacity, in game units.</summary>
internal readonly struct NeedLevel(float current, float capacity)
{
    public float Current => current;

    public float Capacity => capacity;

    public float Room => capacity - current;

    public bool IsBelow(Percent share) => current < share.Of(capacity);
}
