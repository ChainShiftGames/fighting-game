using FightingGame.Online;
using Sim.Data;
using Sim.Data.Sources;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class PlaceholderCharacterSourceTests
{
    private readonly DirectoryDataSource _fixture = new(TestGameData.FixtureDataRoot);

    [Fact]
    public void TakesCharactersFromThePlaceholderWhileTheDataHasNone()
    {
        var source = new PlaceholderCharacterSource(new DataWithoutCharacters(_fixture), _fixture);

        Assert.True(source.UsesPlaceholder);
        Assert.Equal(["fixture_man"], source.ListDirectories("characters"));
        Assert.True(source.FileExists("characters/fixture_man/character.json"));
    }

    [Fact]
    public void IgnoresThePlaceholderOnceTheDataHasCharacters()
    {
        var source = new PlaceholderCharacterSource(_fixture, new DataWithoutCharacters(_fixture));

        Assert.False(source.UsesPlaceholder);
        Assert.Equal(["fixture_man"], source.ListDirectories("characters"));
    }

    [Theory]
    [InlineData("input/buttons.json")]
    [InlineData("input/motions.json")]
    [InlineData("rules/match.json")]
    public void ReadsEverythingElseFromTheRealData(string path)
    {
        // The fixture's copies differ from data/'s in their $schema path, so this tells the two apart.
        var real = new DirectoryDataSource(TestGameData.DataRoot);
        var source = new PlaceholderCharacterSource(new DataWithoutCharacters(real), _fixture);

        Assert.Equal(real.ReadFile(path), source.ReadFile(path));
        Assert.NotEqual(_fixture.ReadFile(path), source.ReadFile(path));
    }

    [Fact]
    public void LoadsAsGameDataWithThePlaceholderCharacter()
    {
        GameData data = TestGameData.WithFixtureCharacter;

        Assert.Equal("fixture_man", Assert.Single(data.Characters).Id);
    }
}
