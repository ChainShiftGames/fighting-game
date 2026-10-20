using System;
using FightingGame.Addons.ControlPanel.Documents;
using Xunit;

namespace FightingGame.Tests.Addons.ControlPanel;

public sealed class JsonPathFieldTests
{
    private const string Move = """
        {
          "id": "5c",
          "frameData": { "startup": 10, "active": 4, "recovery": 20 },
          "boxes": [ { "kind": "hit", "x": -25000 } ],
          "hits": [
            { "damage": 800, "hitstop": 12, "otg": false },
            { "damage": 400, "hitstop": 9, "guard": "mid" }
          ]
        }
        """;

    [Fact]
    public void ReadsNestedAndListedValues()
    {
        Assert.Equal(20, JsonPathField.ReadNumber(Move, "frameData.recovery"));
        Assert.Equal(9, JsonPathField.ReadNumber(Move, "hits[1].hitstop"));
        Assert.Equal(-25000, JsonPathField.ReadNumber(Move, "boxes[0].x"));
        Assert.False(JsonPathField.ReadBool(Move, "hits[0].otg"));
        Assert.Equal("mid", JsonPathField.ReadString(Move, "hits[1].guard"));
    }

    [Fact]
    public void WritingChangesOnlyThatValue()
    {
        string written = JsonPathField.WriteNumber(Move, "hits[1].hitstop", 14, decimals: 0);

        Assert.Equal(Move.Replace("\"hitstop\": 9", "\"hitstop\": 14"), written);
        Assert.Equal(12, JsonPathField.ReadNumber(written, "hits[0].hitstop"));
    }

    [Fact]
    public void WritesFlagsStringsAndDecimals()
    {
        string json = JsonPathField.WriteBool(Move, "hits[0].otg", true);
        json = JsonPathField.WriteString(json, "hits[1].guard", "low");
        json = JsonPathField.WriteNumber(json, "frameData.active", 0.25, decimals: 3);

        Assert.True(JsonPathField.ReadBool(json, "hits[0].otg"));
        Assert.Equal("low", JsonPathField.ReadString(json, "hits[1].guard"));
        Assert.Contains("\"active\": 0.25,", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingOrWrongKindOfValueIsAnError()
    {
        Assert.False(JsonPathField.Has(Move, "hits[2].damage"));
        Assert.True(JsonPathField.Has(Move, "hits[1]"));
        Assert.Throws<FormatException>(() => JsonPathField.ReadNumber(Move, "frameData.cooldown"));
        Assert.Throws<FormatException>(() => JsonPathField.ReadNumber(Move, "id"));
        Assert.Throws<FormatException>(() => JsonPathField.WriteNumber(Move, "hits", 1, decimals: 0));
    }

    [Theory]
    [InlineData(3.0, 3, "3")]
    [InlineData(0.5, 3, "0.5")]
    [InlineData(1.23456, 3, "1.235")]
    [InlineData(-7.6, 0, "-8")]
    public void NumbersAreWrittenWithoutTrailingZeros(double value, int decimals, string text) =>
        Assert.Equal(text, JsonPathField.NumberText(value, decimals));
}
