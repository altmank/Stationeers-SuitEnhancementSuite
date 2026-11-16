using System.Globalization;

namespace SuitEnhancementSuite;

/// <summary>The areas whose extra log lines a player can turn on. Config and console name them in lowercase.</summary>
internal enum LogArea
{
    Consume,
    Swap,
    Power,
    Slots,
    Hud,
}

/// <summary>How log lines name things and write numbers, the same way everywhere.</summary>
internal static class LogText
{
    /// <summary><c>PrefabName#ReferenceId</c>: ties a line to one thing in a save.</summary>
    public static string Thing(string prefabName, long referenceId) =>
        string.Concat(prefabName ?? "?", "#", referenceId.ToString(CultureInfo.InvariantCulture));

    /// <summary>A number with a fixed number of decimals, whatever the player's language.</summary>
    public static string Number(float value, int decimals) => value.ToString("F" + decimals, CultureInfo.InvariantCulture);
}
