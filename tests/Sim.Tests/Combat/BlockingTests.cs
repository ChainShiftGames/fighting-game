using Sim.Characters;
using Sim.Combat;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;
using Xunit;
using static Sim.Tests.Combat.CombatCharacter;

namespace Sim.Tests.Combat;

public sealed class BlockingTests
{
    // The attacker stands left of the defender, so the defender's back is to the right.
    private static readonly FighterState _attacker = At(-30_000, facing: 1);
    private static readonly InputWord _back = new(InputWord.Right);
    private static readonly InputWord _downBack = new(InputWord.Right | InputWord.Down);

    [Theory]
    [InlineData(Guard.Mid, BlockStance.Standing, true)]
    [InlineData(Guard.Mid, BlockStance.Crouching, true)]
    [InlineData(Guard.Low, BlockStance.Standing, false)]
    [InlineData(Guard.Low, BlockStance.Crouching, true)]
    [InlineData(Guard.Overhead, BlockStance.Standing, true)]
    [InlineData(Guard.Overhead, BlockStance.Crouching, false)]
    [InlineData(Guard.Unblockable, BlockStance.Standing, false)]
    [InlineData(Guard.Unblockable, BlockStance.Crouching, false)]
    [InlineData(Guard.Mid, BlockStance.None, false)]
    public void EachGuardIsStoppedOnlyByItsStances(Guard guard, BlockStance stance, bool isBlocked)
    {
        Assert.Equal(isBlocked, Blocking.Stops(stance, guard));
    }

    [Fact]
    public void HoldingBackStandBlocksAndDownBackCrouchBlocks()
    {
        FighterState defender = At(30_000, facing: -1);

        Assert.Equal(BlockStance.Standing, Blocking.StanceOf(in defender, in _attacker, _back));
        Assert.Equal(BlockStance.Crouching, Blocking.StanceOf(in defender, in _attacker, _downBack));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(InputWord.Left)]
    [InlineData(InputWord.Down)]
    [InlineData(InputWord.Left | InputWord.Down)]
    public void NotHoldingBackDoesNotBlock(ushort bits)
    {
        FighterState defender = At(30_000, facing: -1);

        Assert.Equal(BlockStance.None, Blocking.StanceOf(in defender, in _attacker, new InputWord(bits)));
    }

    [Fact]
    public void BackIsAwayFromTheAttackerEvenWhileFacingIt()
    {
        // A crossup: the defender still faces right, away from the attacker that just landed on its left.
        FighterState defender = At(0, facing: 1);

        Assert.Equal(BlockStance.Standing, Blocking.StanceOf(in defender, in _attacker, _back));
        Assert.Equal(BlockStance.None, Blocking.StanceOf(in defender, in _attacker, new InputWord(InputWord.Left)));
    }

    [Fact]
    public void AtTheSameXBackFollowsTheDefendersFacing()
    {
        FighterState defender = At(-30_000, facing: -1);
        FighterState attacker = At(-30_000, facing: -1);

        Assert.Equal(BlockStance.Standing, Blocking.StanceOf(in defender, in attacker, _back));
    }

    [Theory]
    [InlineData(FighterAction.Idle)]
    [InlineData(FighterAction.WalkForward)]
    [InlineData(FighterAction.WalkBack)]
    [InlineData(FighterAction.Crouch)]
    [InlineData(FighterAction.Blockstun)]
    [InlineData(FighterAction.CrouchBlockstun)]
    public void FreeOrBlockingFightersCanBlock(FighterAction action)
    {
        FighterState defender = At(30_000, facing: -1) with { Action = action };

        Assert.Equal(BlockStance.Crouching, Blocking.StanceOf(in defender, in _attacker, _downBack));
    }

    [Theory]
    [InlineData(FighterAction.Attack)]
    [InlineData(FighterAction.Hitstun)]
    [InlineData(FighterAction.Prejump)]
    [InlineData(FighterAction.Dash)]
    [InlineData(FighterAction.Landing)]
    [InlineData(FighterAction.Knockdown)]
    public void BusyFightersCannotBlock(FighterAction action)
    {
        FighterState defender = At(30_000, facing: -1) with { Action = action };

        Assert.Equal(BlockStance.None, Blocking.StanceOf(in defender, in _attacker, _back));
    }

    [Fact]
    public void AirborneFightersCannotBlockYet()
    {
        FighterState defender = At(30_000, facing: -1) with { Action = FighterAction.Jump, IsAirborne = 1 };

        Assert.Equal(BlockStance.None, Blocking.StanceOf(in defender, in _attacker, _back));
    }
}
