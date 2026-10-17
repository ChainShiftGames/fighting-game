using System;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

public sealed class JsonBoolFieldTests
{
    private const string File = "{\n  \"schemaVersion\": 1,\n  \"enabled\": false,\n  \"windowFrames\": 8\n}\n";

    [Fact]
    public void ReadsTheField() => Assert.False(JsonBoolField.Read(File, "enabled"));

    [Fact]
    public void RewritesOnlyTheValue() =>
        Assert.Equal(File.Replace("false", "true", StringComparison.Ordinal), JsonBoolField.Write(File, "enabled", true));

    [Theory]
    [InlineData("windowFrames")]
    [InlineData("missing")]
    public void RejectsAFieldThatIsNoBool(string key)
    {
        Assert.Throws<FormatException>(() => JsonBoolField.Read(File, key));
        Assert.Throws<FormatException>(() => JsonBoolField.Write(File, key, true));
    }
}
