namespace SuitEnhancementSuite;

/// <summary>
/// A waste bag: an open bag that is not full before a folded one, because an open bag cannot be folded again; among
/// open bags the fullest, so it is finished first.
/// </summary>
internal readonly struct WasteOffer : IOffer<WasteOffer>
{
    private readonly bool _open;
    private readonly float _fill;

    private WasteOffer(bool open, float fill)
    {
        _open = open;
        _fill = fill;
    }

    public static WasteOffer Folded => new(open: false, fill: 0f);

    /// <param name="fill">Share of the bag already used, 0 to 1.</param>
    public static WasteOffer Open(float fill) => new(open: true, fill);

    public bool Beats(WasteOffer incumbent)
    {
        if (_open != incumbent._open) return _open;
        return _open && _fill > incumbent._fill;
    }
}
