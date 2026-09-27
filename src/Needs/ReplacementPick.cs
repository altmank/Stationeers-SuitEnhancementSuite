using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace SuitEnhancementSuite;

/// <summary>Weighs the usable items of one inventory walk and keeps the best replacement for a need slot.</summary>
internal abstract class ReplacementPick
{
    public abstract DynamicThing Best { get; }

    public abstract void Clear();

    /// <summary>Weighs <paramref name="candidate"/>, which the need slot finds usable.</summary>
    public abstract void Consider(DynamicThing candidate, Human human);
}

/// <summary>A pick that ranks candidates by the pure offer <typeparamref name="TOffer"/> they map to.</summary>
internal sealed class ReplacementPick<TOffer>(Func<DynamicThing, Human, TOffer> describe) : ReplacementPick
    where TOffer : struct, IOffer<TOffer>
{
    private readonly BestOffer<TOffer, DynamicThing> _best = new();

    public override DynamicThing Best => _best.Item;

    public override void Clear() => _best.Clear();

    public override void Consider(DynamicThing candidate, Human human) => _best.Consider(candidate, describe(candidate, human));
}
