namespace SuitEnhancementSuite;

/// <summary>The best item seen so far in one walk over an inventory. Reused across walks; <see cref="Clear"/> starts a new one.</summary>
internal sealed class BestOffer<TOffer, TItem>
    where TOffer : struct, IOffer<TOffer>
    where TItem : class
{
    private TOffer _offer;

    public TItem Item { get; private set; }

    public void Clear()
    {
        Item = null;
        _offer = default;
    }

    public void Consider(TItem item, TOffer offer)
    {
        if (Item is not null && !offer.Beats(_offer)) return;
        Item = item;
        _offer = offer;
    }
}
