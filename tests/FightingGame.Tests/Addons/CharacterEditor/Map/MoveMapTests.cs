using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using Sim.Data.Sources;
using Xunit;

namespace FightingGame.Tests.Addons.CharacterEditor.Map;

/// <summary>The move map's names, chains, layout and facts, read from Testman's data.</summary>
public sealed class MoveMapTests
{
    private static readonly MoveMapSettings _settings = MoveMapSettings.Parse(File.ReadAllText(
        Path.Combine(TestGameData.RepositoryRoot, "game", "addons", "character_editor", "move_map.json")));

    private readonly CharacterDocument _testman =
        CharacterDocument.Open(new DirectoryDataSource(TestGameData.DataRoot), "testman");

    [Theory]
    [InlineData("5b", "Standing Medium")]
    [InlineData("2a", "Crouching Light")]
    [InlineData("j_c", "Jumping Heavy")]
    [InlineData("5d", "Standing D")]
    [InlineData("236c", "Quarter-circle")]
    [InlineData("236236d", "Super")]
    [InlineData("forward_throw", "Forward throw")]
    [InlineData("walk_forward", "Walk forward")]
    [InlineData("burst", "Burst")]
    public void MovesGetPlainNames(string id, string name) =>
        Assert.Equal(name, MoveNames.Of(id, Move(id), _settings));

    [Fact]
    public void MotionsReadAsArrows()
    {
        Assert.Equal("↓↘→", MoveGlyphs.Arrows("236"));
        Assert.Equal("→↓↘", MoveGlyphs.Arrows("623"));
        Assert.Equal("", MoveGlyphs.Arrows("5"));
    }

    [Fact]
    public void StandingMediumChainsIntoHeaviesJumpSpecialsAndSupers()
    {
        IReadOnlyList<ChainLink> links = MoveChains.From("5b", Move("5b"));

        Assert.Equal(
            [
                new ChainLink("5b", ChainTargetKind.Move, "5c"),
                new ChainLink("5b", ChainTargetKind.Move, "2c"),
                new ChainLink("5b", ChainTargetKind.Move, "5d"),
                new ChainLink("5b", ChainTargetKind.Category, "special"),
                new ChainLink("5b", ChainTargetKind.Category, "super"),
                new ChainLink("5b", ChainTargetKind.Jump, ""),
            ],
            links);
    }

    [Fact]
    public void CombosShowNormalsByButtonThenSpecials()
    {
        MoveMapLayout layout = Layout(MapLens.Combos);

        Assert.Equal(
            ["Light", "Medium", "Heavy", "Other normals", "Specials & super"],
            layout.Columns.Select(column => column.Title));
        Assert.Equal(["5a", "2a"], layout.Columns[0].Entries);
        Assert.Equal(["5b", "2b", MoveMapLayout.JumpEntry], layout.Columns[1].Entries);
        Assert.Equal(["5c", "2c", "j_c", "5d"], layout.Columns[2].Entries);
        Assert.Equal(["burst"], layout.Columns[3].Entries);
        Assert.Equal(["623c", "236c", "214a", "214b", "214c", "236236d"], layout.Columns[4].Entries);
        Assert.Equal(["special"], layout.Groups.Select(group => group.Category));
        Assert.Null(layout.Find("idle"));
    }

    [Fact]
    public void SpecialMovesShowOnlySpecialsAndSupers()
    {
        MoveMapLayout layout = Layout(MapLens.SpecialMoves);

        Assert.Equal(["Specials & super"], layout.Columns.Select(column => column.Title));
        Assert.Null(layout.Find(MoveMapLayout.JumpEntry));
    }

    [Fact]
    public void EverythingAddsThrowsMovementAndReactions()
    {
        MoveMapLayout layout = Layout(MapLens.Everything);

        Assert.Contains(layout.Columns, column => column.Title == "Throws" && column.Entries.Contains("back_throw"));
        Assert.Contains(layout.Columns, column => column.Title == "Movement" && column.Entries.Contains("walk_forward"));
        Assert.Contains(layout.Columns, column => column.Title == "Reactions" && column.Entries.Contains("hitstun"));
        Assert.NotNull(layout.Find("idle"));
    }

    [Fact]
    public void StandingMediumIsPlusTwoOnHitAndMinusTwoOnBlock()
    {
        MoveFacts facts = MoveFacts.Of(Move("5b"));

        Assert.Equal(7, facts.ComesOutOn);
        Assert.Equal(500, facts.Damage);
        Assert.Equal((18, 14), (facts.Hitstun, facts.Blockstun));
        Assert.Equal((2, -2), (facts.OnHit, facts.OnBlock));
        Assert.Equal([7, 8, 9], facts.HitFrames.Order());
    }

    [Fact]
    public void AProjectileComesOutOnItsSpawnFrameWithNoFixedAdvantage()
    {
        JsonObject fireball = Move("236c");
        MoveFacts facts = MoveFacts.Of(fireball);

        Assert.True(facts.HitsFromProjectile);
        Assert.Equal(ProjectileFields.SpawnFrame(ProjectileFields.All(fireball)[0]), facts.ComesOutOn);
        Assert.Null(facts.OnHit);
        Assert.Null(facts.OnBlock);
    }

    [Fact]
    public void ProjectileSpeedAndSpawnAreWrittenInPlace()
    {
        JsonObject projectile = ProjectileFields.All(Move("236c"))[0];

        ProjectileFields.SetSpawnFrame(projectile, 20);
        ProjectileFields.SetSpeedForward(projectile, 9000);
        ProjectileFields.SetSpeedUp(projectile, 500);

        Assert.Equal(20, ProjectileFields.SpawnFrame(projectile));
        Assert.Equal("{\"x\":9000,\"y\":500}", projectile["velocity"]!.ToJsonString());

        ProjectileFields.SetSpeedUp(projectile, 0);
        Assert.Equal("{\"x\":9000}", projectile["velocity"]!.ToJsonString());
    }

    [Fact]
    public void ThePointBlankOpponentTouchesTheMovesBody()
    {
        IReadOnlyList<BoxArea> opponent = PointBlankOpponent.Of(Move("5b"), 1, Move("idle"));

        // Both push boxes are 20 px either side of the origin, so the opponent stands 40 px away.
        Assert.Equal([new BoxArea(15_000, 0, 50_000, 120_000)], opponent);
    }

    private JsonObject Move(string id) => _testman.Move(id)!.Root;

    private MoveMapLayout Layout(MapLens lens) =>
        MoveMapLayout.Of(_testman, _settings, lens, MoveChains.Of(_testman));
}
