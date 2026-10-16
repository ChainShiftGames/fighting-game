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
        Assert.Equal(["236c", "5a", "2b", "j_b", "idle", "crouch"], _fixture.MoveIds.ToArray());
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

    private static MoveData Move(string moveId) => _fixture.Moves[IndexOf(moveId)];

    private static int IndexOf(string moveId) => Array.IndexOf(_fixture.MoveIds.ToArray(), moveId);
}
