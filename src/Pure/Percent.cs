using System;
using System.Globalization;

namespace SuitEnhancementSuite;

/// <summary>A share of a whole, 0 to 100. Values outside that range are clamped; NaN reads as 0.</summary>
internal readonly struct Percent
{
    private Percent(double value) => Value = value;

    public double Value { get; }

    public static Percent From(double value) =>
        new(double.IsNaN(value) ? 0d : Math.Max(0d, Math.Min(100d, value)));

    public double Of(double whole) => whole * Value / 100d;

    public override string ToString() => Value.ToString("0.##", CultureInfo.InvariantCulture) + " %";
}
