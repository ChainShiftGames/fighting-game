using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Input.Motions;
using Sim.State;
using Xunit;

namespace Sim.Tests.Input.Motions;

/// <summary>Motion priority only compares the character's own moves with the same button and stance.</summary>
public sealed class MoveInputMatcherTests
{
    private const ushort A = InputSequence.ButtonA;
    private const ushort C = InputSequence.ButtonC;

    private static readonly MoveData _fireball = Move(MoveStance.Ground, 236, C);
    private static readonly MoveData _dragonPunch = Move(MoveStance.Ground, 623, C);
    private static readonly MoveData _standingC = Move(MoveStance.Ground, 5, C);
    private static readonly MoveData _crouchingC = Move(MoveStance.Ground, 2, C);
    private static readonly MoveData _jumpingC = Move(MoveStance.Air, 5, C);
    private static readonly MoveData _fireballA = Move(MoveStance.Ground, 236, A);

    [Fact]
    public void HigherPriorityMotionOfTheSameButtonWins()
    {
        MoveInputMatcher matcher = Matcher(_fireball, _dragonPunch, _standingC);
        InputHistory history = PressOnLast("6236", C);

        Assert.True(matcher.IsTriggered(in history, in _dragonPunch, 1));
        Assert.False(matcher.IsTriggered(in history, in _fireball, 1));
        Assert.False(matcher.IsTriggered(in history, in _standingC, 1));
    }

    [Fact]
    public void MotionTheCharacterLacksDoesNotBlockALowerOne()
    {
        MoveInputMatcher matcher = Matcher(_fireball, _standingC);
        InputHistory history = PressOnLast("6236", C);

        Assert.True(matcher.IsTriggered(in history, in _fireball, 1));
    }

    [Fact]
    public void MovesOfOtherButtonsDoNotCompete()
    {
        MoveInputMatcher matcher = Matcher(_dragonPunch, _fireballA);
        InputHistory history = PressOnLast("6236", A);

        Assert.True(matcher.IsTriggered(in history, in _fireballA, 1));
    }

    [Fact]
    public void GroundMovesDoNotBlockAirMoves()
    {
        MoveInputMatcher matcher = Matcher(_crouchingC, _standingC, _jumpingC);
        InputHistory history = PressOnLast("2", C);

        Assert.True(matcher.IsTriggered(in history, in _crouchingC, 1));
        Assert.False(matcher.IsTriggered(in history, in _standingC, 1));
        Assert.True(matcher.IsTriggered(in history, in _jumpingC, 1));
    }

    [Fact]
    public void BasicMovesAreNeverTriggered()
    {
        MoveData idle = new() { Category = MoveCategory.Basic, TotalFrames = 1 };
        MoveInputMatcher matcher = Matcher(_standingC, idle);
        InputHistory history = PressOnLast("5", C);

        Assert.False(matcher.IsTriggered(in history, in idle, 1));
    }

    [Fact]
    public void MoveOfAnotherCharacterIsNotTriggered()
    {
        MoveInputMatcher matcher = Matcher(_standingC);
        InputHistory history = PressOnLast("5", A);

        Assert.False(matcher.IsTriggered(in history, Move(MoveStance.Ground, 5, A), 1));
    }

    [Fact]
    public void CheckingAMoveDoesNotAllocate()
    {
        MoveInputMatcher matcher = Matcher(_fireball, _dragonPunch, _standingC);
        InputHistory history = PressOnLast("6236", C);
        matcher.IsTriggered(in history, in _fireball, 1);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int check = 0; check < 100; check++)
        {
            matcher.IsTriggered(in history, in _fireball, 1);
            matcher.IsTriggered(in history, in _standingC, -1);
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void RejectsAMotionTheSetDoesNotDefine()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Matcher(Move(MoveStance.Ground, 412, C)));

        Assert.Contains("412", exception.Message, StringComparison.Ordinal);
    }

    private static InputHistory PressOnLast(string directions, ushort button) =>
        new InputSequence().Directions(directions[..^1]).Press(directions[^1] - '0', button).History;

    private static MoveData Move(MoveStance stance, int motion, ushort button) => new()
    {
        Category = motion > 9 ? MoveCategory.Special : MoveCategory.Normal,
        Stance = stance,
        Motion = motion,
        ButtonMask = button,
        TotalFrames = 1,
    };

    private static MoveInputMatcher Matcher(params MoveData[] moves)
    {
        List<string> ids = [];
        for (int move = 0; move < moves.Length; move++)
        {
            ids.Add($"move_{move}");
        }

        int[] basicStateMoves = new int[Enum.GetValues<BasicState>().Length];
        Array.Fill(basicStateMoves, CharacterData.NoMove);
        CharacterData character = new(
            "test", "Test", default, ids.ToArray(), moves, [], [], [], [], basicStateMoves);
        return new MoveInputMatcher(TestMotions.Parser(), character);
    }
}
