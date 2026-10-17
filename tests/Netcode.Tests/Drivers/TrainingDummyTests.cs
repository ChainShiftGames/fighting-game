using System;
using Netcode.Drivers.Training;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Fighters.States;
using Sim.Input;
using Sim.State;
using Xunit;

namespace Netcode.Tests.Drivers;

/// <summary>The guarding dummies and health refill, on hand-made states rather than whole matches.</summary>
public sealed class TrainingDummyTests
{
    private const int ResetFrames = 20;
    private const int RefillDelayFrames = 60;

    private readonly MatchSimulation _match = TestmanMirror.Create();

    [Fact]
    public void BlockAllDummyStaysNeutralWhenNotAttacked()
    {
        var guard = new GuardingDummy(_match.Player1);
        MatchState state = _match.StartState();

        Assert.Equal(InputWord.Neutral, guard.Next(in state));
    }

    [Fact]
    public void BlockAllDummyCrouchBlocksAMidAwayFromTheAttacker()
    {
        var guard = new GuardingDummy(_match.Player1);
        MatchState state = Attacking("5a");

        Assert.Equal(new InputWord((ushort)(BackOfDummy(in state) | InputWord.Down)), guard.Next(in state));
    }

    [Fact]
    public void BlockAllDummyStandBlocksAnOverhead()
    {
        var guard = new GuardingDummy(_match.Player1);
        MatchState state = Attacking("5d");

        Assert.Equal(new InputWord(BackOfDummy(in state)), guard.Next(in state));
    }

    [Fact]
    public void BlockAfterFirstHitLetsTheFirstHitInThenGuards()
    {
        var dummy = new GuardAfterFirstHitDummy(new GuardingDummy(_match.Player1), ResetFrames);
        MatchState attacked = Attacking("5a");

        Assert.Equal(InputWord.Neutral, dummy.Next(in attacked));

        MatchState hit = attacked;
        hit.Fighters[TrainingSides.Dummy].Action = FighterAction.Hitstun;
        dummy.Next(in hit);

        Assert.NotEqual(InputWord.Neutral, dummy.Next(in attacked));
    }

    [Fact]
    public void BlockAfterFirstHitStopsGuardingOnceFreeForTheResetTime()
    {
        var dummy = new GuardAfterFirstHitDummy(new GuardingDummy(_match.Player1), ResetFrames);
        MatchState hit = Attacking("5a");
        hit.Fighters[TrainingSides.Dummy].Action = FighterAction.Hitstun;
        dummy.Next(in hit);

        // The free frame that reaches the reset time still guards; the one after it doesn't.
        MatchState free = _match.StartState();
        for (int frame = 1; frame < ResetFrames; frame++)
        {
            dummy.Next(in free);
        }

        Assert.NotEqual(InputWord.Neutral, dummy.Next(Attacking("5a")));
        Assert.Equal(InputWord.Neutral, dummy.Next(Attacking("5a")));
    }

    [Fact]
    public void HealthRefillsOnlyAfterTheFighterWasFreeForTheDelay()
    {
        var refill = new HealthRefill([_match.Player1, _match.Player2], RefillDelayFrames);
        MatchState state = Fighting();
        int fullHealth = _match.Player2.Stats.Health;
        state.Fighters[TrainingSides.Dummy].Health = fullHealth / 2;

        for (int frame = 1; frame < RefillDelayFrames; frame++)
        {
            refill.Apply(ref state);
        }

        Assert.Equal(fullHealth / 2, state.Fighters[TrainingSides.Dummy].Health);
        refill.Apply(ref state);
        Assert.Equal(fullHealth, state.Fighters[TrainingSides.Dummy].Health);
    }

    [Fact]
    public void HealthDoesNotRefillDuringACombo()
    {
        var refill = new HealthRefill([_match.Player1, _match.Player2], RefillDelayFrames);
        MatchState state = Fighting();
        int halfHealth = _match.Player2.Stats.Health / 2;
        state.Fighters[TrainingSides.Dummy].Health = halfHealth;
        state.Fighters[TrainingSides.Dummy].Action = FighterAction.Hitstun;

        for (int frame = 0; frame < RefillDelayFrames * 2; frame++)
        {
            refill.Apply(ref state);
        }

        Assert.Equal(halfHealth, state.Fighters[TrainingSides.Dummy].Health);
    }

    private MatchState Fighting()
    {
        MatchState state = _match.StartState();
        state.Round.Phase = RoundPhase.Fighting;
        return state;
    }

    // The training player in the first frame of one of Testman's moves.
    private MatchState Attacking(string moveId)
    {
        MatchState state = Fighting();
        ref FighterState player = ref state.Fighters[TrainingSides.Player];
        player.Action = FighterAction.Attack;
        player.Move = _match.Player1.MoveIds.IndexOf(moveId);
        player.MoveFrame = 1;
        Assert.True(player.Move >= 0, $"Testman has no move {moveId}.");
        return state;
    }

    private static ushort BackOfDummy(in MatchState state) =>
        state.Fighters[TrainingSides.Player].Position.X > state.Fighters[TrainingSides.Dummy].Position.X
            ? InputWord.Left
            : InputWord.Right;
}
