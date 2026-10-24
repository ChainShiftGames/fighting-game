using System.IO;
using FightingGame.Tests.UI.Framework;
using FightingGame.UI.CharacterSelect;
using Xunit;

namespace FightingGame.Tests.UI.CharacterSelect;

public sealed class CharacterSelectFileTests
{
    [Fact]
    public void ReadsTheColoursAndTheDescriptionKey()
    {
        SelectableCharacter? character = CharacterSelectFile.Read(
            "ryu", "Ryu", """{ "selectVersion": 1, "description": "CHARACTER_RYU", "colours": ["#ffffff", "#A0B0C0"] }""", out string error);

        Assert.Equal(string.Empty, error);
        Assert.Equal(["#ffffff", "#A0B0C0"], character!.Colours);
        Assert.Equal("CHARACTER_RYU", character.Description.Value);
        Assert.True(character.Description.IsKey);
    }

    [Theory]
    [InlineData("""{ "selectVersion": 2, "colours": ["#ffffff"] }""", "selectVersion")]
    [InlineData("""{ "selectVersion": 1, "colours": [] }""", "colours needs")]
    [InlineData("""{ "selectVersion": 1, "colours": ["red"] }""", "colours[0]")]
    [InlineData("""{ "selectVersion": 1, "colours": ["#ffffff"], "description": "Plays well" }""", "translation key")]
    [InlineData("""{ "selectVersion": 1, "colours": ["#ffffff"], "palette": 3 }""", "palette")]
    public void InvalidFilesAreRefusedWithTheReason(string json, string reason)
    {
        Assert.Null(CharacterSelectFile.Read("ryu", "Ryu", json, out string error));
        Assert.Contains(reason, error);
    }

    [Fact]
    public void WithoutAFileACharacterHasOneColourAndNoDescription()
    {
        SelectableCharacter placeholder = CharacterSelectFile.Placeholder("ryu", "Ryu");

        Assert.Single(placeholder.Colours);
        Assert.True(placeholder.Description.IsEmpty);
    }

    [Fact]
    public void EveryCharactersSelectFileIsValidAndItsDescriptionIsTranslated()
    {
        string folder = Path.Combine(TestGameData.RepositoryRoot, "game", "Assets", "Characters");
        int files = 0;
        foreach (string path in Directory.EnumerateFiles(folder, "*_select.json", SearchOption.AllDirectories))
        {
            files++;
            SelectableCharacter? character = CharacterSelectFile.Read("id", "Name", File.ReadAllText(path), out string error);
            Assert.True(character is not null, $"{path}: {error}");
            Assert.True(
                character.Description.IsEmpty || MenuTextTable.English.Has(character.Description.Value),
                $"{path}: {character.Description.Value} isn't in menu_text.csv");
        }

        Assert.True(files > 0);
    }
}
