using System.Globalization;

namespace SuitEnhancementSuite;

/// <summary>
/// How much power the Shared Power slots move per power tick: at most <see cref="RatePerTarget"/> joules into each
/// battery, while <see cref="Loss"/> of what is drawn from the source never arrives.
/// </summary>
internal readonly struct PowerSharingPolicy(float ratePerTarget, Percent loss)
{
    /// <summary>Joules per power tick into one battery; the game shows this unit as W, like the Battery Cell Charger.</summary>
    public float RatePerTarget => ratePerTarget > 0f ? ratePerTarget : 0f;

    public Percent Loss => loss;

    /// <summary>Share of drawn energy that arrives, 0 to 1.</summary>
    public float Efficiency => (float)(1d - loss.Value / 100d);

    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.##} W per battery, loss {1}", RatePerTarget, Loss);
}
