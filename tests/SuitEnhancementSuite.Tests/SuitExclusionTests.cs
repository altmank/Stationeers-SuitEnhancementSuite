using Xunit;

namespace SuitEnhancementSuite.Tests;

public class SuitExclusionTests
{
    private const string Suit = "Assets.Scripts.Objects.Clothing.Suit";
    private const string Base = "Assets.Scripts.Objects.DynamicThing";

    [Theory]
    [InlineData("ItemAegisSuit", "Aegis.Game.AegisSuit", Suit, Base)]
    [InlineData("ItemAegisSuitMk2", "Aegis.Game.AegisSuitMk2", "Aegis.Game.AegisSuit", Suit)]
    [InlineData("ItemAegisSuit", Suit, Base)]
    [InlineData(null, "Aegis.Game.AegisSuit")]
    public void Aegis_suits_are_excluded(string prefab, params string[] chain) =>
        Assert.True(SuitExclusion.IsExcluded(chain, prefab));

    [Theory]
    [InlineData("ItemHARMSuit", "Assets.Scripts.Objects.Clothing.Suits.HARMSuit", Suit, Base)]
    [InlineData("ItemHardSuit", "Assets.Scripts.Objects.Clothing.AdvancedSuit", Suit, Base)]
    [InlineData("ItemEvaSuit", Suit, Base)]
    [InlineData("ItemMarineBodyArmor", "Assets.Scripts.Objects.Clothing.BodyArmor", Base)]
    [InlineData("ItemAegisSuitCase", "Some.AegisSuitAddon", Suit)]
    [InlineData("ItemAegisSuit2", "aegis.game.aegissuit")]
    public void Other_clothing_is_not_excluded(string prefab, params string[] chain) =>
        Assert.False(SuitExclusion.IsExcluded(chain, prefab));

    [Fact]
    public void Null_and_empty_inputs_are_not_excluded()
    {
        Assert.False(SuitExclusion.IsExcluded(null, null));
        Assert.False(SuitExclusion.IsExcluded(new string[0], ""));
    }
}
