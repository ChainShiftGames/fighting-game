using Sim.Fighters.States;
using Sim.State;
using Xunit;

namespace Sim.Tests.Match;

public sealed class RoundStartTests
{
    private readonly RoundSetup _setup = new(timerFrames: 600, player1Health: 8_000, player2Health: 12_000);

    [Fact]
    public void FightersStartMirroredAroundTheCentreFacingEachOtherWithFullHealthInIdle()
    {
        MatchState state = _setup.FirstRound();

        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        Assert.Equal((-RoundSetup.StartDistance, 1, 8_000), (player1.Position.X, player1.Facing, player1.Health));
        Assert.Equal((RoundSetup.StartDistance, -1, 12_000), (player2.Position.X, player2.Facing, player2.Health));
        Assert.Equal((FighterAction.Idle, FighterAction.Idle), (player1.Action, player2.Action));
        Assert.Equal((1, 600, RoundPhase.Fighting), (state.Round.Round, state.Round.TimerFramesLeft, state.Round.Phase));
    }

    [Fact]
    public void ANewRoundClearsRoundStateButKeepsWinsAndTheMatchClock()
    {
        MatchState state = _setup.FirstRound();
        state.Frame = 777;
        state.Round.Wins[1] = 1;
        state.Random = RandomState.FromSeed(42);
        state.Fighters[0].StunFramesLeft = 9;
        state.Fighters[1].Cancel.IsPending = 1;
        state.Fighters[1].Timing.TimeScaleFramesLeft = 5;
        state.Round.Phase = RoundPhase.RoundOver;
        state.Round.RoundWinners = Winners.Only(1);

        _setup.Start.Begin(ref state, 2);

        Assert.Equal((777, 1, RandomState.FromSeed(42)), (state.Frame, state.Round.Wins[1], state.Random));
        Assert.Equal(
            (0, 0, 0),
            (state.Fighters[0].StunFramesLeft, state.Fighters[1].Cancel.IsPending,
                state.Fighters[1].Timing.TimeScaleFramesLeft));
        Assert.Equal(
            (2, RoundPhase.Fighting, Winners.None), (state.Round.Round, state.Round.Phase, state.Round.RoundWinners));
    }
}
