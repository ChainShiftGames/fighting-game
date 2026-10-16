using System;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

public sealed class JsonStringFieldTests
{
    private const string File = "{\n  \"schemaVersion\": 1,\n  \"tradeRule\": \"trade\",\n  \"roundsToWin\": 2\n}\n";

    [Fact]
    public void ReadsATopLevelString()
    {
        Assert.Equal("trade", JsonStringField.Read(File, "tradeRule"));
    }

    [Fact]
    public void RewritesOnlyTheString()
    {
        string written = JsonStringField.Write(File, "tradeRule", "clash");

        Assert.Equal(File.Replace("\"trade\",", "\"clash\",", StringComparison.Ordinal), written);
        Assert.Equal("clash", JsonStringField.Read(written, "tradeRule"));
    }

    [Fact]
    public void RewritesAStringHoldingEscapedQuotes()
    {
        const string Escaped = "{ \"name\": \"a \\\"b\\\" c\", \"next\": \"d\" }";

        string written = JsonStringField.Write(Escaped, "name", "e");

        Assert.Equal("{ \"name\": \"e\", \"next\": \"d\" }", written);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("roundsToWin")]
    public void RejectsAMissingOrNonStringField(string key)
    {
        Assert.Throws<FormatException>(() => JsonStringField.Write(File, key, "x"));
    }

    [Fact]
    public void RejectsAKeyThatAlsoAppearsNested()
    {
        const string Nested = "{ \"rule\": \"a\", \"modes\": [{ \"rule\": \"b\" }] }";

        Assert.Throws<FormatException>(() => JsonStringField.Write(Nested, "rule", "c"));
    }
}
