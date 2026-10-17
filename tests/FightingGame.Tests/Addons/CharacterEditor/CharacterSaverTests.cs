using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Sim.Characters;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>Saving writes only data the game accepts, and only the files that changed.</summary>
public sealed class CharacterSaverTests : IDisposable
{
    private const string MoveFile = "characters/testman/moves/5a.json";
    private const string CharacterFile = "characters/testman/character.json";

    private readonly string _root = Directory.CreateTempSubdirectory("character-editor-").FullName;
    private readonly CharacterSaver _saver;

    public CharacterSaverTests()
    {
        CopyFolder(TestGameData.DataRoot, _root);
        _saver = new CharacterSaver(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void AnUnchangedCharacterIsValid()
    {
        Assert.Empty(_saver.Validate(Open()));
    }

    [Fact]
    public void SavingWritesTheChangedFileOnly()
    {
        CharacterDocument document = Open();
        EditedFile move = document.Move("5a")!;
        move.Root["frameData"]!["recovery"] = 10;
        move.MarkChanged();
        string characterBefore = File.ReadAllText(PathOf(CharacterFile));

        IReadOnlyList<DataError> problems = _saver.Save(document);

        Assert.Empty(problems);
        Assert.False(document.HasChanges);
        CharacterData testman = Reload().Character("testman");
        // 5a: startup 5, active 3, so 4 + 3 + 10 frames.
        Assert.Equal(17, testman.Moves[testman.MoveIds.IndexOf("5a")].TotalFrames);
        Assert.Equal(characterBefore, File.ReadAllText(PathOf(CharacterFile)));
    }

    [Fact]
    public void ReorderedMovesAreSavedInTheNewPriority()
    {
        CharacterDocument document = Open();
        string first = document.MoveIds[0];
        string second = document.MoveIds[1];

        document.SwapMoves(0, 1);
        Assert.Empty(_saver.Save(document));

        CharacterData testman = Reload().Character("testman");
        Assert.Equal([second, first], [testman.MoveIds[0], testman.MoveIds[1]]);
    }

    [Fact]
    public void InvalidDataIsReportedAsTheGameReportsItAndNothingIsWritten()
    {
        CharacterDocument document = Open();
        EditedFile move = document.Move("5a")!;
        move.Root["frameData"]!["startup"] = 0;
        move.MarkChanged();
        string before = File.ReadAllText(PathOf(MoveFile));

        IReadOnlyList<DataError> problems = _saver.Save(document);

        DataError problem = Assert.Single(problems);
        Assert.Equal(MoveFile, problem.File);
        Assert.StartsWith("frameData.startup", problem.Field, StringComparison.Ordinal);
        Assert.True(document.HasChanges);
        Assert.Equal(before, File.ReadAllText(PathOf(MoveFile)));
    }

    [Fact]
    public void CrossFileRulesAreCheckedToo()
    {
        CharacterDocument document = Open();
        // Drops 5a from the move list while its file stays: the game rejects unlisted move files.
        JsonArray moves = document.Character.Root["moves"]!.AsArray();
        moves.RemoveAt(IndexOf(document.MoveIds, "5a"));
        document.Character.MarkChanged();

        IReadOnlyList<DataError> problems = _saver.Validate(document);

        Assert.Contains(problems, problem => problem.File == MoveFile);
    }

    private static int IndexOf(IReadOnlyList<string> ids, string id)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == id)
            {
                return i;
            }
        }

        return -1;
    }

    private CharacterDocument Open() => CharacterDocument.Open(new DirectoryDataSource(_root), "testman");

    private GameData Reload() => new GameDataLoader(new DirectoryDataSource(_root)).Load();

    private string PathOf(string file) => Path.Combine(_root, file.Replace('/', Path.DirectorySeparatorChar));

    private static void CopyFolder(string from, string to)
    {
        foreach (string folder in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, folder)));
        }

        foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(to, Path.GetRelativePath(from, file)));
        }
    }
}
