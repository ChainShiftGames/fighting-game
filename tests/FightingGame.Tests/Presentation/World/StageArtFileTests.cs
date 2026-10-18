using FightingGame.Presentation.World;
using Xunit;

namespace FightingGame.Tests.Presentation.World;

public sealed class StageArtFileTests
{
    [Fact]
    public void ReadsTheBackgroundAndWhereItsGroundIs()
    {
        StageArtSettings? art = FileRead(
            """{ "artVersion": 1, "background": "waterfall.webp", "groundLine": 0.25, "parallax": 0.6 }""");

        Assert.Equal(new StageArtSettings("waterfall.webp", 0.25f, 4f, 0.6f), art);
        Assert.Equal("res://Assets/Stages/waterfall.webp", StageArtFile.Resolve(art!.Background));
    }

    [Theory]
    [InlineData("""{ "artVersion": 2 }""", "artVersion")]
    [InlineData("""{ "artVersion": 1, "groundLine": 1.5 }""", "groundLine")]
    [InlineData("""{ "artVersion": 1, "depth": 0 }""", "depth")]
    [InlineData("""{ "artVersion": 1, "parallax": 1 }""", "parallax")]
    [InlineData("""{ "artVersion": 1, "scroll": 2 }""", "")]
    public void RefusesAnInvalidFileWithTheReason(string json, string reason)
    {
        Assert.Null(StageArtFile.Read(json, out string error));
        Assert.NotEmpty(error);
        Assert.Contains(reason, error);
    }

    private static StageArtSettings? FileRead(string json) => StageArtFile.Read(json, out _);
}
