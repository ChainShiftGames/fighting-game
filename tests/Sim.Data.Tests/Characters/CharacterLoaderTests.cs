using System;
using Sim.Characters;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Sim.Math;
using Xunit;

namespace Sim.Data.Tests.Characters;

public sealed class CharacterLoaderTests
{
    private static readonly CharacterData _fixture =
        Fixture.Load(new DirectoryDataSource(Fixture.DataRoot)).Character(Fixture.CharacterId);

    [Fact]
    public void MovesKeepTheOrderOfTheMoveList()
    {
        Assert.Equal(["236c", "5a", "2b", "j_b", "idle", "crouch", "backdash"], _fixture.MoveIds.ToArray());
    }

    [Fact]
    public void StatsAreReadInSubpixels()
    {
        Assert.Equal(10000, _fixture.Stats.Health);
        Assert.Equal(4000, _fixture.Stats.WalkForwardSpeed);
        Assert.Equal(DashStyle.Run, _fixture.Stats.DashStyle);
        Assert.Equal(2, _fixture.Stats.AirActions);
    }

    // Startup counts the first active frame: startup 5, active 3, recovery 8 → active 5–7, 15 frames in total.
    [Fact]
    public void FrameDataBecomesActiveFramesAndTotalLength()
    {
        MoveData move = Move("5a");

        Assert.Equal(5, move.FirstActiveFrame);
        Assert.Equal(7, move.LastActiveFrame);
        Assert.Equal(15, move.TotalFrames);
    }

    [Fact]
    public void BasicMovesHaveADurationAndNoActiveFrames()
    {
        MoveData idle = Move("idle");

        Assert.Equal(MoveCategory.Basic, idle.Category);
        Assert.Equal(60, idle.TotalFrames);
        Assert.Equal(0, idle.FirstActiveFrame);
        Assert.Equal(0, idle.ButtonMask);
    }

    [Fact]
    public void InputBecomesMotionNumberAndButtonBit()
    {
        MoveData special = Move("236c");

        Assert.Equal(236, special.Motion);
        Assert.Equal(1 << 6, special.ButtonMask);
        Assert.Equal(MoveStance.Ground, special.Stance);
        Assert.Equal(MoveStance.Air, Move("j_b").Stance);
    }

    [Fact]
    public void BoxesBecomeAreasFromTheirMinimumCorner()
    {
        ReadOnlySpan<BoxData> boxes = _fixture.BoxesOf(Move("5a"));

        Assert.Equal(4, boxes.Length);
        BoxData hitbox = boxes[3];
        Assert.Equal(BoxKind.Hit, hitbox.Kind);
        Assert.Equal(5, hitbox.FirstFrame);
        Assert.Equal(7, hitbox.LastFrame);
        Assert.Equal(AxisAlignedBox.FromCorner(25000, 80000, 40000, 15000), hitbox.Area);
    }

    [Fact]
    public void CancelTargetsBecomeMoveIndexes()
    {
        ReadOnlySpan<CancelData> cancels = _fixture.CancelsOf(Move("5a"));

        Assert.Equal(2, cancels.Length);
        Assert.Equal(CancelOn.Hit | CancelOn.Block, cancels[0].On);
        Assert.Equal(MoveCategory.Special | MoveCategory.Super, cancels[0].IntoCategories);
        Assert.Equal([IndexOf("2b")], _fixture.TargetsOf(cancels[0]).ToArray());
        Assert.Equal([IndexOf("5a")], _fixture.TargetsOf(cancels[1]).ToArray());
    }

    [Fact]
    public void HitPropertiesAndDefaultsAreRead()
    {
        HitData low = Assert.Single(_fixture.HitsOf(Move("2b")).ToArray());
        HitData special = Assert.Single(_fixture.HitsOf(Move("236c")).ToArray());

        Assert.Equal(Guard.Low, low.Guard);
        Assert.Equal(0, low.ChipDamage);
        Assert.Equal(HitReaction.Hitstun, low.Reaction);
        Assert.Equal(150, special.ChipDamage);
        Assert.Equal(HitReaction.Knockdown, special.Reaction);
        Assert.Empty(_fixture.HitsOf(Move("idle")).ToArray());
    }

    [Fact]
    public void BasicStatesPointAtTheirMoves()
    {
        Assert.Equal(IndexOf("idle"), _fixture.BasicStateMove(BasicState.Idle));
        Assert.Equal(IndexOf("crouch"), _fixture.BasicStateMove(BasicState.Crouch));
        Assert.Equal(CharacterData.NoMove, _fixture.BasicStateMove(BasicState.Knockdown));
    }

    [Fact]
    public void VelocityRangesAreReadInFrameOrderWithYDefaultingToZero()
    {
        VelocityData[] velocities = _fixture.VelocitiesOf(Move("backdash")).ToArray();

        Assert.Equal(
            [
                new VelocityData { FirstFrame = 1, LastFrame = 6, X = -12000, Y = 0 },
                new VelocityData { FirstFrame = 7, LastFrame = 14, X = -6000, Y = 0 },
            ],
            velocities);
        Assert.Empty(_fixture.VelocitiesOf(Move("5a")).ToArray());
    }

    [Fact]
    public void InvulnerableFramesAreRead()
    {
        MoveData backdash = Move("backdash");

        Assert.Equal((1, 7), (backdash.InvulnerableFirstFrame, backdash.InvulnerableLastFrame));
        Assert.True(backdash.IsInvulnerableOn(7));
        Assert.False(backdash.IsInvulnerableOn(8));
        Assert.False(Move("5a").IsInvulnerableOn(1));
    }

    [Fact]
    public void ProjectilesAreReadWithTheirBoxesAndHit()
    {
        ProjectileData projectile = Assert.Single(_fixture.ProjectilesOf(Move("236c")).ToArray());

        Assert.Equal(14, projectile.SpawnFrame);
        Assert.Equal(new IntVector2(50000, 60000), projectile.Offset);
        Assert.Equal(new IntVector2(7000, -500), projectile.Velocity);
        Assert.Equal((90, 2), (projectile.Lifetime, projectile.Durability));
        Assert.Equal((700, 80, 11), (projectile.Hit.Damage, projectile.Hit.ChipDamage, projectile.Hit.Hitstop));
        BoxData box = Assert.Single(_fixture.BoxesOf(projectile).ToArray());
        Assert.Equal(BoxKind.Hit, box.Kind);
        Assert.Equal((1, 90), (box.FirstFrame, box.LastFrame));
        Assert.Equal(AxisAlignedBox.FromCorner(-20000, -15000, 40000, 30000), box.Area);
    }

    [Fact]
    public void ProjectileBoxesStayOutOfTheMovesBoxes()
    {
        Assert.Equal(3, _fixture.BoxesOf(Move("236c")).Length);
        Assert.Empty(_fixture.ProjectilesOf(Move("5a")).ToArray());
        Assert.Single(_fixture.Projectiles.ToArray());
    }

    private static MoveData Move(string moveId) => _fixture.Moves[IndexOf(moveId)];

    private static int IndexOf(string moveId) => Array.IndexOf(_fixture.MoveIds.ToArray(), moveId);
}
