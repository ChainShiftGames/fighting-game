using System;
using System.IO;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

public sealed class DataFileSettingTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("debug-panel-").FullName;
    private readonly DataFileSetting _setting = new("Round start distance", "rules/match.json", "roundStartDistance", 0, 500_000, 1000);

    public DataFileSettingTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "rules"));
        File.WriteAllText(MatchFile, "{\n  \"schemaVersion\": 1,\n  \"roundStartDistance\": 150000\n}\n");
    }

    private string MatchFile => Path.Combine(_root, "rules", "match.json");

    [Fact]
    public void ReadsAndWritesTheValueInItsDataFile()
    {
        Assert.Equal(150_000, _setting.Read(_root));

        _setting.Write(_root, 200_000);

        Assert.Equal(200_000, _setting.Read(_root));
        Assert.Equal("{\n  \"schemaVersion\": 1,\n  \"roundStartDistance\": 200000\n}\n", File.ReadAllText(MatchFile));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(500_001)]
    public void RefusesValuesOutsideItsRange(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _setting.Write(_root, value));
        Assert.Equal(150_000, _setting.Read(_root));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
