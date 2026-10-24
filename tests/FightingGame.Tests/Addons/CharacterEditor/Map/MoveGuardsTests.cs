using System.Linq;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Map;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor.Map;

public sealed class MoveGuardsTests
{
    private static JsonObject Move(string json) => JsonNode.Parse(json)!.AsObject();

    [Fact]
    public void ListsTheMovesOwnHitsThenItsProjectiles()
    {
        JsonObject move = Move("""
            {
              "hits": [{ "guard": "low" }, { "guard": "overhead" }],
              "projectiles": [{ "hit": { "guard": "mid" } }]
            }
            """);

        Assert.Equal(
            [("Hit 1", "low"), ("Hit 2", "overhead"), ("Projectile", "mid")],
            MoveGuards.Of(move).Select(slot => (slot.Label, slot.Guard)));
    }

    [Fact]
    public void ASingleHitIsJustTheHit()
    {
        Assert.Equal("Hit", Assert.Single(MoveGuards.Of(Move("""{ "hits": [{ "guard": "mid" }] }"""))).Label);
    }

    [Fact]
    public void DistinctGivesEachGuardOnceInHitOrder()
    {
        JsonObject move = Move("""{ "hits": [{ "guard": "mid" }, { "guard": "low" }, { "guard": "mid" }] }""");

        Assert.Equal(["mid", "low"], MoveGuards.Distinct(move));
    }

    [Fact]
    public void AMoveWithoutHitsHasNoGuards()
    {
        Assert.Empty(MoveGuards.Distinct(Move("""{ "frameData": { "duration": 10 } }""")));
    }

    [Fact]
    public void SettingAGuardWritesItIntoTheHitInPlace()
    {
        JsonObject move = Move("""{ "hits": [{ "damage": 300, "guard": "mid", "hitstun": 12 }] }""");

        MoveGuards.Set(MoveGuards.Of(move)[0], "overhead");

        Assert.Equal(
            """{"hits":[{"damage":300,"guard":"overhead","hitstun":12}]}""",
            move.ToJsonString());
    }
}
