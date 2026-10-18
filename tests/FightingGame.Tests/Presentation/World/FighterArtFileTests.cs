using FightingGame.Presentation.World;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class FighterArtFileTests
{
    [Fact]
    public void ReadsAModelWithClipNamesAndContactFrames()
    {
        const string Json = """
            {
              "artVersion": 1,
              "art": "model",
              "model": "testman.glb",
              "scale": 0.5,
              "turnDegrees": -90,
              "animations": {
                "5a": { "clip": "Punch", "activeFrom": 6, "recoveryFrom": 10 },
                "idle": { "clip": "Idle" }
              }
            }
            """;

        FighterArtSettings? settings = FighterArtFile.Read(Json, out string error);

        Assert.Equal(string.Empty, error);
        Assert.NotNull(settings);
        Assert.Equal(FighterArtKind.Model, settings.Art);
        Assert.Equal("testman.glb", settings.Model);
        Assert.Equal(0.5f, settings.Scale);
        Assert.Equal(-90f, settings.TurnDegrees);
        Assert.Equal("Punch", settings.ClipFor("5a"));
        Assert.Equal(new ContactFrames(6, 10), settings.ContactsFor("5a"));
        Assert.Null(settings.ContactsFor("idle"));
    }

    [Fact]
    public void ReadsClipsKeptInTheirOwnFiles()
    {
        const string Json = """
            {
              "artVersion": 1,
              "art": "model",
              "model": "res://TestModel/MainModel.fbx",
              "animations": {
                "walk_back": { "file": "res://TestModel/Walking Backwards.fbx", "clip": "mixamo_com", "loop": true, "inPlace": true }
              }
            }
            """;

        AnimationSettings walk = FighterArtFile.Read(Json, out _)!.For("walk_back");

        Assert.Equal("res://TestModel/Walking Backwards.fbx", walk.File);
        Assert.Equal("mixamo_com", walk.Clip);
        Assert.True(walk.Loop);
        Assert.True(walk.InPlace);
    }

    [Fact]
    public void LeavesAMoveWithoutAnEntryToTheModelsClipNamedAfterIt()
    {
        AnimationSettings move = FighterArtFile.Read("""{ "artVersion": 1, "art": "model", "model": "m.glb" }""", out _)!
            .For("2c");

        Assert.Null(move.File);
        Assert.Null(move.Loop);
        Assert.False(move.InPlace);
    }

    [Theory]
    [InlineData("testman.glb", "res://Assets/Characters/testman/testman.glb")]
    [InlineData("res://TestModel/MainModel.fbx", "res://TestModel/MainModel.fbx")]
    public void FindsNamedFilesInTheCharactersFolderUnlessGivenAResPath(string file, string path)
    {
        Assert.Equal(path, FighterArtFile.Resolve("testman", file));
    }

    [Fact]
    public void NamesAMovesClipAfterTheMoveWhenTheFileDoesNot()
    {
        FighterArtSettings? settings = FighterArtFile.Read("""{ "artVersion": 1, "art": "model", "model": "m.glb" }""", out _);

        Assert.Equal("2c", settings!.ClipFor("2c"));
        Assert.Equal(1f, settings.Scale);
    }

    [Fact]
    public void ReadsAPlaceholderWithoutAModel()
    {
        FighterArtSettings? settings = FighterArtFile.Read("""{ "artVersion": 1, "art": "placeholder" }""", out _);

        Assert.Equal(FighterArtKind.Placeholder, settings!.Art);
    }

    [Theory]
    [InlineData("""{ "artVersion": 2, "art": "model", "model": "m.glb" }""", "artVersion")]
    [InlineData("""{ "artVersion": 1, "art": "model" }""", "no model file")]
    [InlineData("""{ "artVersion": 1, "art": "sprites" }""", "")]
    [InlineData("""{ "artVersion": 1, "art": "placeholder", "colour": "red" }""", "")]
    [InlineData("""{ "artVersion": 1, "art": "model", "model": "m.glb", "scale": 0 }""", "scale")]
    [InlineData(
        """{ "artVersion": 1, "art": "model", "model": "m.glb", "animations": { "5a": { "activeFrom": 4 } } }""",
        "animations.5a")]
    [InlineData(
        """{ "artVersion": 1, "art": "model", "model": "m.glb", "animations": { "5a": { "activeFrom": 8, "recoveryFrom": 4 } } }""",
        "animations.5a")]
    [InlineData("not json", "")]
    public void RefusesAnInvalidFileWithTheReason(string json, string reason)
    {
        FighterArtSettings? settings = FighterArtFile.Read(json, out string error);

        Assert.Null(settings);
        Assert.NotEmpty(error);
        Assert.Contains(reason, error);
    }
}
