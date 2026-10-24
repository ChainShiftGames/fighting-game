using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Presentation.World;
using FightingGame.Presentation.World.Sparks;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>The spark editor saves into the art file without touching anything but the move's <c>sparks</c> entry.</summary>
public sealed class ArtSparkTextTests
{
    private const string File = """
        {
          "artVersion": 1,
          "art": "model",
          "model": "m.glb",
          "animations": {},
          "sparks": {
            // Kick: high.
            "5d": [{ "angle": 10, "style": "tear" }],
            // Sweep: low.
            "2c": [{ "offset": { "x": 0, "y": -10 } }]
          }
        }
        """;

    [Fact]
    public void RewritesOnlyTheMovesEntry()
    {
        string written = ArtSparkText.Write(File, "5d", [new HitSparkSettings(Angle: 20, OffsetX: 5, Scale: 1.5f)]);

        Assert.Equal(
            File.Replace("""[{ "angle": 10, "style": "tear" }]""", """[{ "angle": 20, "offset": { "x": 5, "y": 0 }, "scale": 1.5 }]"""),
            written);
    }

    [Fact]
    public void AddsANewMoveAtTheEnd()
    {
        string written = ArtSparkText.Write(File, "5b", [new HitSparkSettings(), new HitSparkSettings(Style: HitSparkStyle.Burst)]);

        Assert.Contains("""
            "2c": [{ "offset": { "x": 0, "y": -10 } }],
                "5b": [{}, { "style": "burst" }]
            """, written, System.StringComparison.Ordinal);
        Assert.Equal(HitSparkStyle.Burst, Read(written).Sparks["5b"][1].Style);
    }

    [Fact]
    public void AllDefaultsRemoveTheEntryAndItsComment()
    {
        string first = ArtSparkText.Write(File, "5d", [new HitSparkSettings()]);
        string last = ArtSparkText.Write(File, "2c", [new HitSparkSettings()]);

        Assert.DoesNotContain("Kick", first, System.StringComparison.Ordinal);
        Assert.False(Read(first).Sparks.ContainsKey("5d"));
        Assert.True(Read(first).Sparks.ContainsKey("2c"));
        Assert.DoesNotContain("Sweep", last, System.StringComparison.Ordinal);
        Assert.False(Read(last).Sparks.ContainsKey("2c"));
        Assert.Equal(10f, Read(last).Sparks["5d"][0].Angle);
    }

    [Fact]
    public void AddsTheSparksObjectWhenTheFileHasNone()
    {
        const string NoSparks = """
            {
              "artVersion": 1,
              "art": "model",
              "model": "m.glb",
              "animations": {}
            }
            """;

        string written = ArtSparkText.Write(NoSparks, "5a", [new HitSparkSettings(OffsetY: 12)]);

        Assert.Equal(12f, Read(written).Sparks["5a"][0].OffsetY);
        Assert.Equal(NoSparks, ArtSparkText.Write(NoSparks, "5a", [new HitSparkSettings()]));
    }

    private static FighterArtSettings Read(string text) =>
        FighterArtFile.Read(text, out string error) ?? throw new Xunit.Sdk.XunitException(error);
}
