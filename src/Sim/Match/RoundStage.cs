using Sim.Events;
using Sim.Fighters.States;
using Sim.Math;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Sim.Match;

/// <summary>The round and timer step of the tick: runs the timer, ends rounds by KO or time-out, starts the next.</summary>
/// <remarks>
/// Runs last in the frame, so it sees the frame's hits fully resolved. While fighting:
/// - KO first: a fighter at 0 health ends the round (<see cref="RoundJudge"/>), also on the timer's last frame.
/// - Otherwise the timer counts down one frame, except during a global freeze (<see cref="FrameActivity.IsFrozen"/>);
///   hitstop doesn't stop it. Counting down to 0 is a time-out. A timer already at 0 is no time limit.
/// A round's result and round wins (<see cref="MatchScore"/>) are fixed on the frame it ends. Then comes the
/// round-over pause of <see cref="MatchRules.RoundOverFrames"/> frames, counted on every frame (frozen or not): the
/// logic keeps running so a KO'd fighter can finish falling and landing, which presentation needs to show the KO,
/// but input is neutral (<see cref="RoundInputLock"/>). Damage taken during the pause doesn't change the result.
/// After the pause the next round starts (<see cref="RoundStart"/>), or the match is over: the phase stays
/// <see cref="RoundPhase.MatchOver"/> for good and the logic keeps ticking deterministically with neutral input.
/// </remarks>
public sealed class RoundStage : ITickStage
{
    private readonly RoundStart _start;
    private readonly RoundJudge _judge;
    private readonly MatchScore _score;
    private readonly int _roundOverFrames;

    public RoundStage(RoundStart start, RoundJudge judge, MatchScore score, in MatchRules rules)
    {
        _start = start;
        _judge = judge;
        _score = score;
        _roundOverFrames = rules.RoundOverFrames;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        if (state.Round.Phase == RoundPhase.Fighting)
        {
            Fight(ref state, activity.IsFrozen);
        }
        else if (state.Round.Phase == RoundPhase.RoundOver)
        {
            RunRoundOverPause(ref state);
        }
    }

    private void Fight(ref MatchState state, bool isFrozen)
    {
        Winners knockOutWinners = RoundJudge.KnockOutWinners(in state.Fighters);
        if (knockOutWinners.IsDecided)
        {
            EmitKnockOuts(ref state);
            EndRound(ref state.Round, RoundEnding.KnockOut, knockOutWinners);
            return;
        }

        if (isFrozen || state.Round.TimerFramesLeft <= 0)
        {
            return;
        }

        if (--state.Round.TimerFramesLeft == 0)
        {
            state.Events.Emit(state.Frame, FrameEventKind.TimeOut, FrameEvent.NoFighter, IntVector2.Zero, 0);
            EndRound(ref state.Round, RoundEnding.TimeOut, _judge.TimeOutWinners(in state.Fighters));
        }
    }

    private static void EmitKnockOuts(ref MatchState state)
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            if (KnockOut.IsKnockedOut(in state.Fighters[fighter]))
            {
                state.Events.Emit(
                    state.Frame, FrameEventKind.KnockOut, fighter, state.Fighters[fighter].Position, 0);
            }
        }
    }

    private void EndRound(ref RoundState round, RoundEnding ending, Winners winners)
    {
        round.Phase = RoundPhase.RoundOver;
        round.Ending = ending;
        round.RoundWinners = winners;
        round.RoundOverFramesLeft = _roundOverFrames;
        _score.Award(ref round, winners);
    }

    private void RunRoundOverPause(ref MatchState state)
    {
        if (--state.Round.RoundOverFramesLeft > 0)
        {
            return;
        }

        state.Round.RoundOverFramesLeft = 0;
        Winners matchWinners = _score.Result(in state.Round);
        if (matchWinners.IsDecided)
        {
            state.Round.Phase = RoundPhase.MatchOver;
            state.Round.MatchWinners = matchWinners;
            state.Events.Emit(
                state.Frame, FrameEventKind.MatchEnd, FrameEvent.NoFighter, IntVector2.Zero, matchWinners.Players);
            return;
        }

        int round = state.Round.Round + 1;
        _start.Begin(ref state, round);
        state.Events.Emit(state.Frame, FrameEventKind.RoundStart, FrameEvent.NoFighter, IntVector2.Zero, round);
    }
}
