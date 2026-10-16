using System;
using System.IO;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

public sealed class DataFileChoiceTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("debug-panel-").FullName;
    private readonly DataFileChoice _setting = new("Trade rule", "rules/match.json", "tradeRule", ["trade", "clash"]);

    public DataFileChoiceTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "rules"));
        WriteMatchFile("trade");
    }

    private string MatchFile => Path.Combine(_root, "rules", "match.json");

    [Fact]
    public void ReadsAndWritesTheChoiceInItsDataFile()
    {
        Assert.Equal("trade", _setting.Read(_root));

        _setting.Write(_root, "clash");

        Assert.Equal("clash", _setting.Read(_root));
        Assert.Equal("{\n  \"schemaVersion\": 1,\n  \"tradeRule\": \"clash\"\n}\n", File.ReadAllText(MatchFile));
    }

    [Fact]
    public void RefusesANameThatIsNotAChoice()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _setting.Write(_root, "Clash"));
        Assert.Equal("trade", _setting.Read(_root));
    }

    [Fact]
    public void RejectsAFileHoldingANameThatIsNotAChoice()
    {
        WriteMatchFile("both");

        Assert.Throws<FormatException>(() => _setting.Read(_root));
    }

    [Theory]
    [InlineData("trade", 0)]
    [InlineData("clash", 1)]
    [InlineData("none", -1)]
    public void FindsAChoicesPosition(string name, int index)
    {
        Assert.Equal(index, _setting.IndexOf(name));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void WriteMatchFile(string tradeRule) =>
        File.WriteAllText(MatchFile, $"{{\n  \"schemaVersion\": 1,\n  \"tradeRule\": \"{tradeRule}\"\n}}\n");
}
