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

        DataFileSetting[] settings = ArtFileSettingList.LookSettings(_root);

        Assert.Equal(["ann/ann_art.json", "zed/zed_art.json"], Array.ConvertAll(settings, setting => setting.File));
        Assert.Equal(10, settings[0].Read(_root));
        Assert.Equal((0, 90), (settings[0].Min, settings[0].Max));
    }

    [Fact]
    public void ListsTheReHitFadeAndTheClipStartOfEachAnimatedReaction()
    {
        WriteArtFile("ann", """
            {
              "art": "model",
              "animations": {
                "idle": { "clip": "idle" },
                "hitstun": { "clip": "hit", "speed": 1.5 },
                "air_hitstun": { "clip": "air", "clipFrom": 4, "clipTo": 30 }
              }
            }
            """);

        DataFileSetting[] settings = ArtFileSettingList.LookSettings(_root);

        Assert.Equal(
            ["reHitFadeFrames", "animations.hitstun.clipFrom", "animations.air_hitstun.clipFrom"],
            Array.ConvertAll(settings, setting => setting.Key));
        Assert.Equal([0, 0, 4], Array.ConvertAll(settings, setting => setting.Read(_root)));
        Assert.Equal(29, settings[2].Max);
    }

    [Fact]
    public void SavingAReactionsClipStartAddsItAndLeavesUnchangedRowsOut()
    {
        WriteArtFile("ann", "{ \"animations\": { \"hitstun\": { \"clip\": \"hit\" } } }");
        DataFileSetting[] settings = ArtFileSettingList.LookSettings(_root);

        settings[0].Write(_root, 0);
        settings[1].Write(_root, 20);

        Assert.Equal(
            "{ \"animations\": { \"hitstun\": { \"clipFrom\": 20, \"clip\": \"hit\" } } }",
            File.ReadAllText(Path.Combine(_root, "ann", "ann_art.json")));
    }

    [Fact]
    public void ListsNothingWithoutTheFolder()
    {
        Assert.Empty(ArtFileSettingList.LookSettings(Path.Combine(_root, "missing")));
    }

    private void WriteArtFile(string id, string json)
    {
        Directory.CreateDirectory(Path.Combine(_root, id));
        File.WriteAllText(Path.Combine(_root, id, $"{id}_art.json"), json);
    }
}
