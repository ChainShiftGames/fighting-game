using System;
using System.IO;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

public sealed class ArtFileSettingListTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("art-files-").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void ListsTheCameraTurnOfEachArtFileThatHasOne()
    {
        WriteArtFile("zed", "{ \"art\": \"model\", \"cameraTurnDegrees\": 30 }");
        WriteArtFile("ann", "{\n  // a comment\n  \"cameraTurnDegrees\": 10\n}");
        WriteArtFile("box", "{ \"art\": \"placeholder\" }");
        Directory.CreateDirectory(Path.Combine(_root, "no_art_file"));

        DataFileSetting[] settings = ArtFileSettingList.CameraTurns(_root);

        Assert.Equal(["ann/ann_art.json", "zed/zed_art.json"], Array.ConvertAll(settings, setting => setting.File));
        Assert.Equal(10, settings[0].Read(_root));
        Assert.Equal((0, 90), (settings[0].Min, settings[0].Max));
    }

    [Fact]
    public void ListsNothingWithoutTheFolder()
    {
        Assert.Empty(ArtFileSettingList.CameraTurns(Path.Combine(_root, "missing")));
    }

    private void WriteArtFile(string id, string json)
    {
        Directory.CreateDirectory(Path.Combine(_root, id));
        File.WriteAllText(Path.Combine(_root, id, $"{id}_art.json"), json);
    }
}
