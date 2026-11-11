using System;

namespace SuitEnhancementSuite;

/// <summary>
/// Decides from a prefab name alone whether an item counts as a suit: any of the markers "Suit", "Icarus" or "Armor"
/// occurring anywhere in the name, case-insensitive. This is the name fallback used when the runtime type does not
/// already decide (the type chain catches known suit classes; the name catches everything else).
/// </summary>
public static class SuitNames
{
    private static readonly string[] Markers = { "Suit", "Icarus", "Armor" };

    /// <summary>
    /// True when <paramref name="prefabName"/> contains any marker, compared case-insensitively; false for a null or
    /// empty name. The name fallback used when the runtime type does not already decide.
    /// </summary>
    public static bool LooksLikeSuit(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return false;
        for (var i = 0; i < Markers.Length; i++)
            if (prefabName.IndexOf(Markers[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }
}
