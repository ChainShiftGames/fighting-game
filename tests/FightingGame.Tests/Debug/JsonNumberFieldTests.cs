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

    [Fact]
    public void ReadsAndRewritesAFileWithComments()
    {
        const string Commented = "{\n  // Look settings\n  \"cameraTurnDegrees\": 30,\n  \"turnFrames\": 6\n}\n";

        string written = JsonNumberField.Write(Commented, "cameraTurnDegrees", 45);

        Assert.Equal(Commented.Replace("30", "45", StringComparison.Ordinal), written);
    }

    [Theory]
    [InlineData("buttonBuffer")]
    [InlineData("movementInputs")]
    public void RejectsAMissingOrNonNumberField(string key)
    {
        Assert.Throws<FormatException>(() => JsonNumberField.Write(File, key, 1));
    }

    [Fact]
    public void RewritesTheTopLevelFieldThoughTheKeyAlsoAppearsNested()
    {
        const string Nested = "{ \"motions\": [{ \"window\": 6 }], \"window\": 3 }";

        Assert.Equal("{ \"motions\": [{ \"window\": 6 }], \"window\": 4 }", JsonNumberField.Write(Nested, "window", 4));
    }

    [Fact]
    public void ReadsAndRewritesANestedFieldByItsPath()
    {
        const string Art = "{\n  // 2° turn\n  \"animations\": {\n    \"idle\": { \"clipFrom\": 5 },\n" +
            "    \"hitstun\": { \"clip\": \"hit\", \"clipFrom\": 10 }\n  }\n}\n";

        string written = JsonNumberField.Write(Art, "animations.hitstun.clipFrom", 22);

        Assert.Equal(Art.Replace("\"clipFrom\": 10", "\"clipFrom\": 22", StringComparison.Ordinal), written);
        Assert.Equal(5, JsonNumberField.Read(written, "animations.idle.clipFrom"));
    }

    [Fact]
    public void AddsAMissingFieldFirstInItsObject()
    {
        const string Art = "{ \"animations\": { \"hitstun\": { \"clip\": \"hit\" }, \"block\": {} } }";

        Assert.Equal(0, JsonNumberField.ReadOr(Art, "animations.hitstun.clipFrom", 0));
        string written = JsonNumberField.WriteOrAdd(Art, "animations.hitstun.clipFrom", 20);
        written = JsonNumberField.WriteOrAdd(written, "animations.block.clipFrom", 3);

        Assert.Equal(
            "{ \"animations\": { \"hitstun\": { \"clipFrom\": 20, \"clip\": \"hit\" }, \"block\": { \"clipFrom\": 3 } } }", written);
    }

    [Fact]
    public void WontAddAFieldWhoseObjectIsMissing()
    {
        Assert.Throws<FormatException>(() => JsonNumberField.ReadOr(File, "animations.hitstun.clipFrom", 0));
        Assert.Throws<FormatException>(() => JsonNumberField.WriteOrAdd(File, "animations.hitstun.clipFrom", 1));
    }
}
