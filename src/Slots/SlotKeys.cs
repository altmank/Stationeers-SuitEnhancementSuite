namespace SuitEnhancementSuite;

/// <summary>
/// StringKeys of the added slots. Saves reference slots by index, not by key; the keys identify the slots in code and
/// give them their labels (the game looks labels up by <c>Animator.StringToHash(key)</c>).
/// </summary>
internal static class SlotKeys
{
    public const string Water = "SuitEnhancementSuiteWater";
    public const string Food = "SuitEnhancementSuiteFood";
    public const string FirstUniformStorage = "SuitEnhancementSuiteUniformStorage1";
    public const string FirstSharedPower = "SuitEnhancementSuiteSharedPower1";
    public const string SecondSharedPower = "SuitEnhancementSuiteSharedPower2";

    /// <summary>Unrestricted storage slot <paramref name="number"/> (1 to 8) of body armor.</summary>
    public static string ArmorStorage(int number) => $"SuitEnhancementSuiteArmorStorage{number}";

    /// <summary>Unrestricted storage slot <paramref name="number"/> (1 to 4) of a uniform.</summary>
    public static string UniformStorage(int number) => $"SuitEnhancementSuiteUniformStorage{number}";

    /// <summary>
    /// Allocation-free: the slots carry these very string instances, so the comparison ends at the reference check.
    /// </summary>
    public static bool IsSharedPower(string key) => key == FirstSharedPower || key == SecondSharedPower;
}
