namespace SuitEnhancementSuite;

/// <summary>A water container: the one holding the most water wins, so the Water slot is refilled least often.</summary>
internal readonly struct WaterOffer(float litres) : IOffer<WaterOffer>
{
    public float Litres => litres;

    public bool Beats(WaterOffer incumbent) => litres > incumbent.Litres;
}
