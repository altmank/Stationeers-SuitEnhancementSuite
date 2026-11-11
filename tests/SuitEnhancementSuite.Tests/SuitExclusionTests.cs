using Xunit;

namespace SuitEnhancementSuite.Tests;

public class SuitExclusionTests
{
    private const string Suit = "Assets.Scripts.Objects.Clothing.Suit";
    private const string Helmet = "Assets.Scripts.Objects.Items.Helmet";
    private const string Base = "Assets.Scripts.Objects.DynamicThing";

    [Theory]
    [InlineData("ItemSuitAegis", "Aegis.Game.AegisSuit", Suit, Base)]
    [InlineData("ItemAegisSuitMk2", "Aegis.Game.AegisSuitMk2", "Aegis.Game.AegisSuit", Suit)]
    [InlineData("ItemSuitAegis", Suit, Base)]
    [InlineData("ItemSuitHelmetAegis", Helmet, Base)]
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
    [InlineData("ItemAegisSuit", Suit, Base)]
    [InlineData("ItemSuitHelmetHARM", Helmet, Base)]
    [InlineData("ItemAegisThruster", Base)]
    [InlineData("ItemSuitAegisCase", Base)]
    [InlineData("ItemSuitaegis", Suit, Base)]
    [InlineData("ItemSuitHelmetaegis", Helmet, Base)]
    public void Other_clothing_is_not_excluded(string prefab, params string[] chain) =>
        Assert.False(SuitExclusion.IsExcluded(chain, prefab));

    [Fact]
    public void Null_and_empty_inputs_are_not_excluded()
    {
        Assert.False(SuitExclusion.IsExcluded(null, null));
        Assert.False(SuitExclusion.IsExcluded(new string[0], ""));
    }
}
