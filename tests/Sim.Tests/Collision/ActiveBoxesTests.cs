using System.Collections.Generic;
using Sim.Characters;
using Sim.Collision;
using Sim.Math;
using Sim.State;
using Xunit;
using static Sim.Tests.Collision.BoxedCharacter;

namespace Sim.Tests.Collision;

public sealed class ActiveBoxesTests
{
    private static readonly CharacterData _character = Create();

    [Fact]
    public void OnlyBoxesOfTheKindOnTheCurrentFrameArePlacedAtTheFighter()
    {
        FighterState fighter = At(100_000, facing: 1, Jab);

        Assert.Empty(Collect(in fighter, BoxKind.Hit));
        fighter.MoveFrame = 2;
        Assert.Equal([new AxisAlignedBox(120_000, 70_000, 100_000 + HitReach, 90_000)], Collect(in fighter, BoxKind.Hit));
        fighter.MoveFrame = 4;
        Assert.Empty(Collect(in fighter, BoxKind.Hit));
    }

    [Fact]
    public void BoxesOfAFighterFacingLeftAreMirrored()
    {
        FighterState fighter = At(100_000, facing: -1, Jab);
        fighter.MoveFrame = 2;

        Assert.Equal([new AxisAlignedBox(100_000 - HitReach, 70_000, 80_000, 90_000)], Collect(in fighter, BoxKind.Hit));
    }

    [Fact]
    public void PushboxIsTheUnionOfThePushboxesOnTheFrame()
    {
        FighterState fighter = At(0, facing: 1, Jab);

        Assert.Equal(new AxisAlignedBox(-PushHalfWidth, 0, 30_000, 140_000), Pushbox.Of(_character, in fighter));
    }

    [Fact]
    public void AFrameWithoutPushboxesGivesAnEmptyBoxAtTheOrigin()
    {
        FighterState fighter = At(50_000, facing: 1, PassThrough, y: 10_000);

        AxisAlignedBox pushbox = Pushbox.Of(_character, in fighter);

        Assert.True(pushbox.IsEmpty);
        Assert.Equal(new AxisAlignedBox(50_000, 10_000, 50_000, 10_000), pushbox);
    }

    [Fact]
    public void HitboxesTouchHurtboxesOnlyWhileActiveAndInReach()
    {
        FighterState attacker = At(0, facing: 1, Jab);
        FighterState close = At(HitReach, facing: -1);
        FighterState far = At(HitReach + 25_000, facing: -1);

        Assert.False(BoxContact.Overlaps(_character, in attacker, BoxKind.Hit, _character, in close, BoxKind.Hurt));
        attacker.MoveFrame = 2;
        Assert.True(BoxContact.Overlaps(_character, in attacker, BoxKind.Hit, _character, in close, BoxKind.Hurt));
        Assert.False(BoxContact.Overlaps(_character, in attacker, BoxKind.Hit, _character, in far, BoxKind.Hurt));
    }

    private static List<AxisAlignedBox> Collect(in FighterState fighter, BoxKind kind)
    {
        var boxes = new List<AxisAlignedBox>();
        foreach (AxisAlignedBox box in new ActiveBoxes(_character, in fighter, kind))
        {
            boxes.Add(box);
        }

        return boxes;
    }
}
