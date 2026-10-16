using System;
using Sim.Events;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Match;

public sealed class RoundStageTests
{
    private static readonly FrameActivity _frozen = new(advancingFighters: 0, isFrozen: true);
    private static readonly FrameActivity _bothInHitstop = new(advancingFighters: 0, isFrozen: false);

    private readonly RoundSetup _setup = new(timerFrames: 600, roundOverFrames: 10);

    [Fact]
    public void TheTimerCountsDownOneFramePerFrame()
    {
        MatchState state = _setup.FirstRound();

        Run(ref state, 3);

        Assert.Equal(597, state.Round.TimerFramesLeft);
    }

    [Fact]
    public void TheTimerStopsDuringAGlobalFreeze()
    {
        MatchState state = _setup.FirstRound();

        _setup.Stage.Run(ref state, in _frozen);

        Assert.Equal(600, state.Round.TimerFramesLeft);
    }

    [Fact]
    public void HitstopDoesNotStopTheTimer()
    {
        MatchState state = _setup.FirstRound();

        _setup.Stage.Run(ref state, in _bothInHitstop);

        Assert.Equal(599, state.Round.TimerFramesLeft);
    }

    [Fact]
    public void ATimerAtZeroIsNoTimeLimit()
    {
        RoundSetup untimed = new(timerFrames: 0);
        MatchState state = untimed.FirstRound();

        for (int frame = 0; frame < 100; frame++)
        {
            untimed.Stage.Run(ref state, FrameActivity.Everyone);
        }

        Assert.Equal((RoundPhase.Fighting, 0), (state.Round.Phase, state.Round.TimerFramesLeft));
    }

    [Fact]
    public void TheTimerRunningOutIsATimeOutWonOnHealth()
    {
        MatchState state = _setup.FirstRound();
        state.Round.TimerFramesLeft = 1;
        state.Fighters[0].Health = 4_000;
        state.Fighters[1].Health = 5_000;

        Run(ref state, 1);

        Assert.Equal(
            (RoundPhase.RoundOver, RoundEnding.TimeOut, Winners.Only(1)),
            (state.Round.Phase, state.Round.Ending, state.Round.RoundWinners));
        Assert.Equal(1, state.Round.Wins[1]);
        Assert.Equal(1, CountEvents(in state, FrameEventKind.TimeOut, FrameEvent.NoFighter));
    }

    [Fact]
    public void AKnockOutOnTheTimersLastFrameIsAKnockOut()
    {
        MatchState state = _setup.FirstRound();
        state.Round.TimerFramesLeft = 1;
        state.Fighters[0].Health = 0;

        Run(ref state, 1);

        Assert.Equal((RoundEnding.KnockOut, Winners.Only(1)), (state.Round.Ending, state.Round.RoundWinners));
        Assert.Equal(0, CountEvents(in state, FrameEventKind.TimeOut, FrameEvent.NoFighter));
    }

    [Fact]
    public void AKnockOutEndsTheRoundWithAnEventPerKnockedOutFighter()
    {
        MatchState state = _setup.FirstRound();
        state.Fighters[0].Health = 0;
        state.Fighters[1].Health = 0;

        Run(ref state, 1);

        Assert.Equal(
            (RoundPhase.RoundOver, RoundEnding.KnockOut, true),
            (state.Round.Phase, state.Round.Ending, state.Round.RoundWinners.IsDraw));
        Assert.Equal(1, CountEvents(in state, FrameEventKind.KnockOut, 0));
        Assert.Equal(1, CountEvents(in state, FrameEventKind.KnockOut, 1));
    }

    [Fact]
    public void TheTimerStopsDuringTheRoundOverPause()
    {
        MatchState state = KnockedOutPlayer2();
        int timer = state.Round.TimerFramesLeft;

        Run(ref state, 5);

        Assert.Equal(timer, state.Round.TimerFramesLeft);
    }

    [Fact]
    public void TheNextRoundStartsResetAfterTheRoundOverPause()
    {
        MatchState state = KnockedOutPlayer2();
        state.Fighters[0].Position = new(90_000, 30_000);
        state.Fighters[0].Timing.HitstopFramesLeft = 4;
        state.Camera.X = 50_000;
        state.Freeze.FramesLeft = 3;

        Run(ref state, 9);
        Assert.Equal(RoundPhase.RoundOver, state.Round.Phase);
        Run(ref state, 1);

        Assert.Equal((RoundPhase.Fighting, 2, 600), (state.Round.Phase, state.Round.Round, state.Round.TimerFramesLeft));
        Assert.Equal((1, 0), (state.Round.Wins[0], state.Round.Wins[1]));
        Assert.Equal((-RoundSetup.StartDistance, 0), (state.Fighters[0].Position.X, state.Fighters[0].Position.Y));
        Assert.Equal(RoundSetup.StartDistance, state.Fighters[1].Position.X);
        Assert.Equal((RoundSetup.Health, RoundSetup.Health), (state.Fighters[0].Health, state.Fighters[1].Health));
        Assert.Equal((0, 0, 0), (state.Fighters[0].Timing.HitstopFramesLeft, state.Camera.X, state.Freeze.FramesLeft));
        Assert.Equal((RoundEnding.None, Winners.None), (state.Round.Ending, state.Round.RoundWinners));
        Assert.Single(state.Events.OfFrame(state.Frame).ToArray(), e => e.Kind == FrameEventKind.RoundStart && e.Detail == 2);
    }

    [Fact]
    public void TheMatchEndsAfterThePauseOnceAPlayerHasEnoughWinsAndThenStaysOver()
    {
        MatchState state = _setup.FirstRound();
        state.Round.Wins[0] = 1;
        state.Fighters[1].Health = 0;
        Run(ref state, 1);

        Run(ref state, 10);

        Assert.Equal((RoundPhase.MatchOver, Winners.Only(0)), (state.Round.Phase, state.Round.MatchWinners));
        Assert.Single(
            state.Events.OfFrame(state.Frame).ToArray(),
            e => e.Kind == FrameEventKind.MatchEnd && e.Detail == Winners.Only(0).Players);
        MatchState over = state;
        Run(ref state, 1000);
        over.Frame = state.Frame;
        Assert.Equal(StateChecksum.Compute(in over), StateChecksum.Compute(in state));
    }

    [Fact]
    public void MirroringThePlayersMirrorsTheResult()
    {
        MatchState left = _setup.FirstRound();
        MatchState right = _setup.FirstRound();
        left.Fighters[0].Health = 0;
        right.Fighters[1].Health = 0;

        Run(ref left, 1);
        Run(ref right, 1);

        Assert.Equal((Winners.Only(1), Winners.Only(0)), (left.Round.RoundWinners, right.Round.RoundWinners));
        Assert.Equal((left.Round.Wins[1], left.Round.Wins[0]), (right.Round.Wins[0], right.Round.Wins[1]));
    }

    [Fact]
    public void RunningWholeMatchesAllocatesNothing()
    {
        MatchState state = _setup.FirstRound();
        Run(ref state, 1);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < 5000; frame++)
        {
            state.Fighters[frame % 2].Health = frame % 50 == 0 ? 0 : state.Fighters[frame % 2].Health;
            _setup.Stage.Run(ref state, FrameActivity.Everyone);
            state.Frame++;
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private MatchState KnockedOutPlayer2()
    {
        MatchState state = _setup.FirstRound();
        state.Fighters[1].Health = 0;
        Run(ref state, 1);
        return state;
    }

    // Each run is a new frame: the counter moves on first, so the events of the latest run are those of state.Frame.
    private void Run(ref MatchState state, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            state.Frame++;
            _setup.Stage.Run(ref state, FrameActivity.Everyone);
        }
    }

    private static int CountEvents(in MatchState state, FrameEventKind kind, int fighter)
    {
        int count = 0;
        foreach (FrameEvent frameEvent in state.Events.OfFrame(state.Frame))
        {
            count += frameEvent.Kind == kind && frameEvent.Fighter == fighter ? 1 : 0;
        }

        return count;
    }
}
