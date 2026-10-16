using System;
using System.Collections.Generic;
using System.Linq;
using FightingGame.Presentation;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.State;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class FighterBoxesTests
{
    private static readonly CharacterData _character = TestGameData.FixtureMan;

    [Fact]
    public void ShowsOnlyTheBoxesOfTheCurrentMoveFrame()
    {
        // fixture_man's 5a: hitbox on frames 5-7.
        Assert.DoesNotContain(Collect(Attack("5a", frame: 4, facing: 1)), box => box.Kind == BoxKind.Hit);
        Assert.Contains(Collect(Attack("5a", frame: 5, facing: 1)), box => box.Kind == BoxKind.Hit);
        Assert.DoesNotContain(Collect(Attack("5a", frame: 8, facing: 1)), box => box.Kind == BoxKind.Hit);
    }

    [Fact]
    public void MirrorsBoxesForAFighterFacingLeftAndPlacesThemAtItsPosition()
    {
        FighterState right = Attack("5a", frame: 5, facing: 1);
        FighterState left = right with { Facing = -1 };

        AxisAlignedBox hitRight = Collect(right).Single(box => box.Kind == BoxKind.Hit).Area;
        AxisAlignedBox hitLeft = Collect(left).Single(box => box.Kind == BoxKind.Hit).Area;

        Assert.True(hitRight.MinX > right.Position.X);
        Assert.True(hitLeft.MaxX < left.Position.X);
        Assert.Equal(hitRight.MinX - right.Position.X, left.Position.X - hitLeft.MaxX);
        Assert.Equal(hitRight.MinY, hitLeft.MinY);
    }

    [Fact]
    public void TakesTheBodyFromThePushboxes()
    {
        FighterState fighter = Attack("5a", frame: 5, facing: 1);
        List<WorldBox> boxes = Collect(fighter);

        AxisAlignedBox body = FighterBoxes.Body(boxes);

        Assert.Equal(boxes.Single(box => box.Kind == BoxKind.Push).Area, body);
    }

    [Fact]
    public void TakesTheBodyFromTheHurtboxesWithoutPushboxes()
    {
        WorldBox[] hurt =
        [
            new(BoxKind.Hurt, new AxisAlignedBox(0, 0, 10, 50)),
            new(BoxKind.Hurt, new AxisAlignedBox(5, 40, 30, 60)),
        ];

        Assert.Equal(new AxisAlignedBox(0, 0, 30, 60), FighterBoxes.Body([.. hurt]));
        Assert.True(FighterBoxes.Body([]).IsEmpty);
    }

    private static List<WorldBox> Collect(in FighterState fighter)
    {
        List<WorldBox> boxes = [];
        FighterBoxes.Collect(_character, in fighter, boxes);
        return boxes;
    }

    private static FighterState Attack(string moveId, int frame, int facing) => new()
    {
        Position = new IntVector2(70_000, 0),
        Facing = facing,
        Action = FighterAction.Attack,
        Move = _character.MoveIds.IndexOf(moveId),
        MoveFrame = frame,
    };
}
