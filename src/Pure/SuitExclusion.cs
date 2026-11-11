using System;
using System.Collections.Generic;

namespace SuitEnhancementSuite;

/// <summary>Clothing from other mods that SES leaves alone (the Aegis suit): matched by exact name, never by substring.</summary>
public static class SuitExclusion
{
    public const string AegisSuitTypeName = "Aegis.Game.AegisSuit";
    public const string AegisSuitPrefabName = "ItemAegisSuit";

    /// <summary>True when a type in the chain is the Aegis suit, or the prefab is the Aegis suit's.</summary>
    public static bool IsExcluded(IEnumerable<string> typeFullNames, string prefabName)
    {
        if (string.Equals(prefabName, AegisSuitPrefabName, StringComparison.Ordinal)) return true;
        if (typeFullNames == null) return false;
        foreach (var name in typeFullNames)
            if (string.Equals(name, AegisSuitTypeName, StringComparison.Ordinal)) return true;
        return false;
    }
}
