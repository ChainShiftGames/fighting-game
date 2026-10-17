using System.Collections.Generic;
using System.Text;
using FightingGame.Addons.CharacterEditor.Documents;
using Sim.Data.Sources;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>Unsaved files look to Sim.Data exactly as if they had been saved.</summary>
public sealed class OverlayDataSourceTests
{
    private readonly DirectoryDataSource _disk = new(TestGameData.DataRoot);

    [Fact]
    public void AnUnsavedFileReplacesTheOneOnDisk()
    {
        const string MoveFile = "characters/testman/moves/5a.json";
        OverlayDataSource source = new(_disk, Files((MoveFile, "{}")));

        Assert.Equal("{}", Encoding.UTF8.GetString(source.ReadFile(MoveFile)));
        Assert.Equal(_disk.ReadFile("characters/testman/moves/5b.json"), source.ReadFile("characters/testman/moves/5b.json"));
    }

    [Fact]
    public void NewFilesAndFoldersAreListedInOrder()
    {
        OverlayDataSource source = new(_disk, Files(
            ("characters/testman/moves/5aa.json", "{}"),
            ("characters/aaa/character.json", "{}")));

        Assert.True(source.FileExists("characters/testman/moves/5aa.json"));
        IReadOnlyList<string> moves = source.ListFiles("characters/testman/moves");
        Assert.Equal(_disk.ListFiles("characters/testman/moves").Count + 1, moves.Count);
        Assert.Equal("5aa.json", moves[IndexOf(moves, "5a.json") + 1]);
        Assert.Equal(["aaa", .. _disk.ListDirectories("characters")], source.ListDirectories("characters"));
        Assert.DoesNotContain("aaa", source.ListFiles("characters"));
    }

    private static Dictionary<string, byte[]> Files(params (string Path, string Text)[] files)
    {
        Dictionary<string, byte[]> bytes = [];
        foreach ((string path, string text) in files)
        {
            bytes[path] = Encoding.UTF8.GetBytes(text);
        }

        return bytes;
    }

    private static int IndexOf(IReadOnlyList<string> names, string name)
    {
        for (int i = 0; i < names.Count; i++)
        {
            if (names[i] == name)
            {
                return i;
            }
        }

        return -1;
    }
}
