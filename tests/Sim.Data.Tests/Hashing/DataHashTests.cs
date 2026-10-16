using System;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Sim.Data.Hashing;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Xunit;

namespace Sim.Data.Tests.Hashing;

/// <summary>
/// P1-07 done-when: same data → same hash on Windows and Linux. The hash covers only compact integer data (see
/// DataHashBuilder), so file formatting can't reach it; the golden value pins it across platforms because CI runs
/// these tests on Linux and the nightly job and developers run them on Windows.
/// </summary>
public sealed class DataHashTests
{
    // Change only on purpose: a new value means every online player and replay sees different game data.
    // Last change (P1-23): rules/match.json gained the throw values and moves (MoveData) a throw release point.
    private const string FixtureHash = "e53f5718d4f31e4c3d8fef21ca40b0edf4968455ab415819e09a2cce8f85c107";

    private static readonly DataHash _fixtureHash = Fixture.Load(Fixture.InMemory()).Hash;

    [Fact]
    public void FixtureHashMatchesTheGoldenValue()
    {
        Assert.Equal(FixtureHash, _fixtureHash.ToString());
    }

    [Fact]
    public void FilesOnDiskHashLikeTheInMemoryCopy()
    {
        Assert.Equal(_fixtureHash, Fixture.Load(new DirectoryDataSource(Fixture.DataRoot)).Hash);
    }

    [Fact]
    public void WindowsLineEndingsDoNotChangeTheHash()
    {
        InMemoryDataSource data = Fixture.InMemory();
        foreach (string path in data.Paths.ToArray())
        {
            data.SetText(path, data.GetText(path).Replace("\n", "\r\n", StringComparison.Ordinal));
        }

        Assert.Equal(_fixtureHash, Fixture.Load(data).Hash);
    }

    [Fact]
    public void ByteOrderMarkDoesNotChangeTheHash()
    {
        InMemoryDataSource data = Fixture.InMemory();
        foreach (string path in data.Paths.ToArray())
        {
            data.Set(path, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(data.GetText(path))]);
        }

        Assert.Equal(_fixtureHash, Fixture.Load(data).Hash);
    }

    [Fact]
    public void KeyOrderAndWhitespaceDoNotChangeTheHash()
    {
        InMemoryDataSource data = Fixture.InMemory();
        foreach (string path in data.Paths.ToArray())
        {
            JsonNode reordered = ReverseKeys(JsonNode.Parse(data.GetText(path))!);
            data.SetText(path, reordered.ToJsonString());
        }

        Assert.Equal(_fixtureHash, Fixture.Load(data).Hash);
    }

    [Fact]
    public void DisplayNamesDoNotChangeTheHash()
    {
        InMemoryDataSource data = Fixture.InMemory();
        data.Replace(Fixture.CharacterFile, "\"Fixture Man\"", "\"Fixture Person\"");

        Assert.Equal(_fixtureHash, Fixture.Load(data).Hash);
    }

    [Theory]
    [InlineData("input/buttons.json", "\"bit\": 9", "\"bit\": 10")]
    [InlineData("characters/fixture_man/moves/5a.json", "\"damage\": 300", "\"damage\": 301")]
    [InlineData("characters/fixture_man/moves/5a.json", "\"button\": \"a\"", "\"button\": \"b\"")]
    [InlineData("characters/fixture_man/moves/2b.json", "\"x\": 30000", "\"x\": 30001")]
    [InlineData("characters/fixture_man/character.json", "\"gravity\": 1000", "\"gravity\": 999")]
    [InlineData("characters/fixture_man/character.json", "[\"236c\", \"5a\"", "[\"5a\", \"236c\"")]
    [InlineData("characters/fixture_man/moves/236c.json", "\"durability\": 2", "\"durability\": 3")]
    [InlineData("rules/match.json", "\"entitiesPerPlayer\": 4", "\"entitiesPerPlayer\": 3")]
    [InlineData("rules/match.json", "\"counterHitExtraHitstun\": 2", "\"counterHitExtraHitstun\": 3")]
    [InlineData("rules/match.json", "\"throwTechWindowFrames\": 5", "\"throwTechWindowFrames\": 6")]
    [InlineData("rules/match.json", "\"throwVsStrike\": \"throw\"", "\"throwVsStrike\": \"strike\"")]
    [InlineData("rules/match.json", "\"punishCounterExtraDamagePercent\": 20", "\"punishCounterExtraDamagePercent\": 25")]
    [InlineData(
        "characters/fixture_man/moves/5a.json",
        "\"pushback\": 12000",
        "\"pushback\": 12000, \"counterHit\": { \"extraHitstun\": 2, \"extraHitstop\": 2, \"extraDamagePercent\": 20 }")]
    public void GameplayValuesChangeTheHash(string path, string oldText, string newText)
    {
        InMemoryDataSource data = Fixture.InMemory();
        data.Replace(path, oldText, newText);

        Assert.NotEqual(_fixtureHash, Fixture.Load(data).Hash);
    }

    [Fact]
    public void HashPrintsAsSha256Hex()
    {
        using DataHashBuilder builder = new();

        // SHA-256 of no bytes, as printed by sha256sum.
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", builder.Finish().ToString());
    }

    private static JsonNode ReverseKeys(JsonNode node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                JsonObject reversed = [];
                foreach ((string key, JsonNode? value) in jsonObject.Reverse().ToArray())
                {
                    jsonObject.Remove(key);
                    reversed[key] = value is null ? null : ReverseKeys(value);
                }

                return reversed;
            case JsonArray jsonArray:
                JsonArray copy = [];
                foreach (JsonNode? element in jsonArray.ToArray())
                {
                    jsonArray.Remove(element);
                    copy.Add(element is null ? null : ReverseKeys(element));
                }

                return copy;
            default:
                return node;
        }
    }
}
