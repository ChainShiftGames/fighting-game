using System;
using FightingGame.Debug;
using Xunit;

namespace FightingGame.Tests.Debug;

public sealed class JsonNumberFieldTests
{
    private const string File = "{\n  \"schemaVersion\": 1,\n  \"motionWindow\": 12,\n  \"movementInputs\": { \"dash\": \"66\" }\n}\n";

    [Fact]
    public void ReadsATopLevelWholeNumber()
    {
        Assert.Equal(12, JsonNumberField.Read(File, "motionWindow"));
    }

    [Fact]
    public void RewritesOnlyTheNumber()
    {
        string written = JsonNumberField.Write(File, "motionWindow", -8);

        Assert.Equal(File.Replace("\"motionWindow\": 12", "\"motionWindow\": -8", StringComparison.Ordinal), written);
        Assert.Equal(-8, JsonNumberField.Read(written, "motionWindow"));
    }

    [Theory]
    [InlineData("buttonBuffer")]
    [InlineData("movementInputs")]
    public void RejectsAMissingOrNonNumberField(string key)
    {
        Assert.Throws<FormatException>(() => JsonNumberField.Write(File, key, 1));
    }

    [Fact]
    public void RejectsAKeyThatAlsoAppearsNested()
    {
        const string Nested = "{ \"window\": 3, \"motions\": [{ \"window\": 6 }] }";

        Assert.Throws<FormatException>(() => JsonNumberField.Write(Nested, "window", 4));
    }
}
