using System;
using System.Collections.Generic;

namespace SuitEnhancementSuite;

/// <summary>Clothing from other mods that SES leaves alone (the Aegis suit and helmet): matched by exact name, never by substring.</summary>
public static class SuitExclusion
{
    public const string AegisSuitTypeName = "Aegis.Game.AegisSuit";
    public const string AegisSuitPrefabName = "ItemSuitAegis";
    public const string AegisHelmetPrefabName = "ItemSuitHelmetAegis";

    private static readonly HashSet<string> ExcludedPrefabNames = new(StringComparer.Ordinal)
    {
        AegisSuitPrefabName,
        AegisHelmetPrefabName,
    };

    /// <summary>True when a type in the chain is the Aegis suit, or the prefab is one of Aegis's clothing prefabs.</summary>
    public static bool IsExcluded(IEnumerable<string> typeFullNames, string prefabName) =>
        IsExcludedPrefab(prefabName) || IsExcludedType(typeFullNames);

    public static bool IsExcludedPrefab(string prefabName) =>
        !string.IsNullOrEmpty(prefabName) && ExcludedPrefabNames.Contains(prefabName);

    public static bool IsExcludedType(IEnumerable<string> typeFullNames)
    {
        if (typeFullNames == null) return false;
        foreach (var name in typeFullNames)
            if (string.Equals(name, AegisSuitTypeName, StringComparison.Ordinal)) return true;
        return false;
    }
}
