using System;
using System.Collections.Generic;
using System.Text;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Debug;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

public sealed class CharacterDocumentTests
{
    private readonly DirectoryDataSource _disk = new(TestGameData.DataRoot);

    [Fact]
    public void ListsTheMovesInTheGamesPriorityOrder()
    {
        CharacterDocument document = CharacterDocument.Open(_disk, "testman");

        Assert.Equal(TestGameData.Testman.MoveIds.ToArray(), document.MoveIds);
        Assert.Equal("characters/testman/moves/5a.json", document.Move("5a")!.Path);
        Assert.Equal("5a", document.Move("5a")!.Root["id"]!.GetValue<string>());
        Assert.False(document.HasChanges);
    }

    [Fact]
    public void SwappingMovesChangesTheCharacterFile()
    {
        CharacterDocument document = CharacterDocument.Open(_disk, "testman");
        string first = document.MoveIds[0];
        string second = document.MoveIds[1];

        document.SwapMoves(0, 1);

        Assert.Equal([second, first], [document.MoveIds[0], document.MoveIds[1]]);
        Assert.True(document.Character.IsChanged);
        Assert.True(document.HasChanges);
    }

    [Fact]
    public void SwappingOutsideTheListIsRefused()
    {
        CharacterDocument document = CharacterDocument.Open(_disk, "testman");

        Assert.Throws<ArgumentOutOfRangeException>(() => document.SwapMoves(0, document.MoveIds.Count));
    }

    [Fact]
    public void AMissingCharacterCantBeOpened()
    {
        DataValidationException exception = Assert.Throws<DataValidationException>(
            () => CharacterDocument.Open(_disk, "nobody"));

        Assert.Equal("characters/nobody/character.json", Assert.Single(exception.Errors).File);
    }

    [Theory]
    [InlineData("{ \"id\": \"5a\", }")]
    [InlineData("{ \"id\": \"5a\", \"id\": \"5b\" }")]
    [InlineData("[1, 2]")]
    public void AMoveFileThatIsntStrictJsonIsReported(string text)
    {
        const string MoveFile = "characters/testman/moves/5a.json";
        OverlayDataSource source = new(_disk, new Dictionary<string, byte[]> { [MoveFile] = Encoding.UTF8.GetBytes(text) });

        DataValidationException exception = Assert.Throws<DataValidationException>(
            () => CharacterDocument.Open(source, "testman"));

        Assert.Equal(MoveFile, Assert.Single(exception.Errors).File);
    }

    [Fact]
    public void AListedMoveWithoutAFileHasNoDocument()
    {
        const string CharacterFile = "characters/testman/character.json";
        string text = Encoding.UTF8.GetString(_disk.ReadFile(CharacterFile)).Replace("\"5a\",", "\"5a\", \"5z\",");
        OverlayDataSource source = new(_disk, new Dictionary<string, byte[]> { [CharacterFile] = Encoding.UTF8.GetBytes(text) });

        CharacterDocument document = CharacterDocument.Open(source, "testman");

        Assert.Contains("5z", document.MoveIds);
        Assert.Null(document.Move("5z"));
    }
}
