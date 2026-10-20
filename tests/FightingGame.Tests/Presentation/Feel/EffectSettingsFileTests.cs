using System.IO;
using FightingGame.Presentation.Feel;
using Xunit;

namespace FightingGame.Tests.Presentation.Feel;

public sealed class EffectSettingsFileTests
{
    private static readonly string _shipped = File.ReadAllText(Path.Combine(
        TestGameData.RepositoryRoot, "game", EffectSettingsFile.ProjectPath.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void TheShippedFileReads()
    {
        EffectSettings? settings = EffectSettingsFile.Read(_shipped, out string error);

        Assert.Equal(string.Empty, error);
        Assert.NotNull(settings);
    }

    [Fact]
    public void AStandardFileReadsAsWritten()
    {
        EffectSettings? settings = EffectSettingsFile.Read(StandardFile(), out string error);

        Assert.Equal(string.Empty, error);
        Assert.Equal(TestEffectSettings.Standard, settings);
    }

    [Theory]
    [InlineData("\"hitPixels\": 3", "\"hitPixels\": 99", "'screenShake.hitPixels' is 99; it must be 0 to 40")]
    [InlineData("\"lifetimeFrames\": 8", "\"lifetimeFrames\": 8.5", "'screenShake.lifetimeFrames' must be a whole number")]
    [InlineData("\"scale\": 1", "\"scale\": 1, \"glow\": 2", "unknown field 'glow'")]
    [InlineData("\"techWeight\": 0.3,", "", "'hitSparks.techWeight' must be a number")]
    [InlineData("\"heavyHitstop\": 20", "\"heavyHitstop\": 8", "'hitSparks.heavyHitstop' must be above 'hitSparks.lightHitstop'")]
    [InlineData("\"schemaVersion\": 1", "\"schemaVersion\": 2", "'schemaVersion' must be 1")]
    public void AnInvalidFileIsRefusedWithTheReason(string field, string replacement, string reason)
    {
        string file = StandardFile();
        Assert.Contains(field, file);

        EffectSettings? settings = EffectSettingsFile.Read(file.Replace(field, replacement), out string error);

        Assert.Null(settings);
        Assert.Equal(reason, error);
    }

    [Fact]
    public void BrokenJsonIsRefused()
    {
        Assert.Null(EffectSettingsFile.Read("{ \"schemaVersion\": ", out string error));
        Assert.NotEqual(string.Empty, error);
    }

    // The standard settings written out, one field per line like the shipped file.
    private static string StandardFile() => """
        {
          "schemaVersion": 1,
          "screenShake": {
            "lifetimeFrames": 8,
            "hitPixels": 3,
            "counterPixels": 5,
            "throwPixels": 4,
            "knockOutPixels": 8,
            "verticalShare": 0.5
          },
          "hitSparks": {
            "lightHitstop": 8,
            "heavyHitstop": 20,
            "counterWeight": 0.3,
            "blockShare": 0.6,
            "tearFromWeight": 0.5,
            "techWeight": 0.3,
            "scale": 1
          },
          "rumble": {
            "heavyStrength": 0.8,
            "lightStrength": 0.3,
            "hitSeconds": 0.15,
            "knockOutSeconds": 0.4
          }
        }
        """;
}
