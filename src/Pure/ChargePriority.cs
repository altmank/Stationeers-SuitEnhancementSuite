namespace SuitEnhancementSuite;

/// <summary>Order in which the Shared Power slots charge batteries: lower values first.</summary>
internal enum ChargePriority : byte
{
    SuitBattery = 0,
    Hand = 1,
    Other = 2,
}

/// <summary>Where a battery sits in a player's inventory, as far as sharing power is concerned.</summary>
internal enum BatteryPlace : byte
{
    /// <summary>One of the worn advanced suit's Shared Power slots: a source, never charged.</summary>
    SharedPower,

    /// <summary>The battery slot of the suit the player wears.</summary>
    SuitBattery,

    /// <summary>A battery slot of something held in a hand.</summary>
    Hand,

    /// <summary>Any other battery slot in the inventory: helmet, backpack contents, tool belt, a tool in the uniform.</summary>
    Elsewhere,
}

internal static class BatteryPlaces
{
    /// <summary>
    /// Whether a battery at <paramref name="place"/> is charged, and in which order. A Shared Power cell is never a
    /// target, so the slots cannot feed each other or themselves.
    /// </summary>
    public static bool TryGetPriority(this BatteryPlace place, bool chargeSuitBattery, out ChargePriority priority)
    {
        switch (place)
        {
            case BatteryPlace.SuitBattery when chargeSuitBattery:
                priority = ChargePriority.SuitBattery;
                return true;
            case BatteryPlace.Hand:
                priority = ChargePriority.Hand;
                return true;
            case BatteryPlace.Elsewhere:
                priority = ChargePriority.Other;
                return true;
            default:
                priority = ChargePriority.Other;
                return false;
        }
    }
}
