using System;
using System.IO;
using UnityEngine;

namespace SuitEnhancementSuite;

/// <summary>
/// Empty-slot icons of the Water, Food and Shared Power slots, embedded from src\Assets. They are drawn like the
/// game's own slot icons (96 x 96, 100 pixels per unit). An icon that cannot be read stays null: the slot then shows
/// its class icon (the battery icon for Shared Power) or none.
/// </summary>
internal static class SlotIcons
{
    private const float PixelsPerUnit = 100f;

    public static Sprite Water { get; private set; }

    public static Sprite Food { get; private set; }

    public static Sprite SharedPower { get; private set; }

    public static void Load()
    {
        Water = LoadEmbedded("SuitEnhancementSuite.Assets.Water.png");
        Food = LoadEmbedded("SuitEnhancementSuite.Assets.Food.png");
        SharedPower = LoadEmbedded("SuitEnhancementSuite.Assets.SharedPower.png");
    }

    private static Sprite LoadEmbedded(string resourceName)
    {
        try
        {
            using var stream = typeof(SlotIcons).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return Missing(resourceName, "resource not found");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, memory.ToArray(), true)) return Missing(resourceName, "not a PNG");
            texture.name = resourceName;
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), PixelsPerUnit);
        }
        catch (Exception e)
        {
            return Missing(resourceName, e.Message);
        }
    }

    private static Sprite Missing(string resourceName, string reason)
    {
        Plugin.Log?.LogWarning($"Slot icon {resourceName} unavailable ({reason}); the slot shows its class icon.");
        return null;
    }
}
