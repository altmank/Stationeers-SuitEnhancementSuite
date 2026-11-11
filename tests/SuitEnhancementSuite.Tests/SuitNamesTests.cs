using Xunit;

namespace SuitEnhancementSuite.Tests;

public class SuitNamesTests
{
    [Theory]
    [InlineData("ItemEvaSuit")]
    [InlineData("ItemHardSuit")]
    [InlineData("ItemHARMSuit")]
    [InlineData("ItemSuitAegis")]
    [InlineData("ItemSuitHelmetHARM")]
    [InlineData("ItemSuitHelmetAegis")]
    [InlineData("ItemMarineBodyArmor")]
    [InlineData("ItemIcarusSuit")]
    [InlineData("itemevasuit")]
    [InlineData("ITEMARMOR")]
    [InlineData("Suit")]
    [InlineData("XICARUSX")]
    public void Suit_prefab_names_look_like_suits(string prefabName) =>
        Assert.True(SuitNames.LooksLikeSuit(prefabName));

    [Theory]
    [InlineData("ItemSpaceHelmet")]
    [InlineData("ItemGasMask")]
    [InlineData("ItemAegisThruster")]
    [InlineData("ItemJetpackBasic")]
    [InlineData("ItemUniformOrange")]
    [InlineData("ItemSui")]
    [InlineData("ItemArmo")]
    [InlineData("Icaru")]
    [InlineData("ItemS uit")]
    public void Non_suit_prefab_names_do_not_look_like_suits(string prefabName) =>
        Assert.False(SuitNames.LooksLikeSuit(prefabName));

    [Fact]
    public void Null_and_empty_names_do_not_look_like_suits()
    {
        Assert.False(SuitNames.LooksLikeSuit(null));
        Assert.False(SuitNames.LooksLikeSuit(""));
    }
}
