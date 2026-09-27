using Assets.Scripts;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Label and empty-slot icon of the added slots that have them: Water, Food, Waste and the two Shared Power slots. Storage
/// slots have neither. Labels are Czech when the game language is Czech, else English.
/// </summary>
internal static class LabelledSlots
{
    public static readonly string[] Keys = [SlotKeys.Water, SlotKeys.Food, SlotKeys.Waste, SlotKeys.FirstSharedPower, SlotKeys.SecondSharedPower];

    public static bool TryGet(string key, out string label, out Sprite icon)
    {
        var czech = Localization.CurrentLanguage == LanguageCode.CS;
        (label, icon) = key switch
        {
            SlotKeys.Water => (czech ? "Voda" : "Water", SlotIcons.Water),
            SlotKeys.Food => (czech ? "Jídlo" : "Food", SlotIcons.Food),
            SlotKeys.Waste => (czech ? "Odpad" : "Waste", SlotIcons.Waste),
            SlotKeys.FirstSharedPower or SlotKeys.SecondSharedPower =>
                (czech ? "Sdílená energie" : "Shared Power", SlotIcons.SharedPower),
            _ => (null, null),
        };
        return label != null;
    }
}
