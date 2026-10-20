using Sim.State;

namespace Sim.Pipeline;

/// <summary>Decides who advances this frame (global freeze, hitstop, time scale) and runs those clocks.</summary>
/// <remarks>
/// Rules (ADR-0011 core capabilities), the same for both fighters:
/// - A global freeze stops every fighter not exempted; while frozen, a fighter's hitstop and time scale don't run.
/// - Time scale is the fighter's own clock: at Numerator/Denominator the fighter gets a frame of its own on that
///   share of match frames. Its duration counts match frames the fighter isn't frozen for.
/// - Hitstop counts the fighter's own frames, so a slowed fighter's hitstop lasts longer, as in slowdown.
/// - A fighter advances on its own frames that aren't spent in hitstop.
/// - A frame a running fighter skips (hitstop, or no own frame under a time scale) is marked in its input history
///   (<see cref="InputHistory.MarkNewestSkipped"/>), so it doesn't age the fighter's buffered presses and motions:
///   input made during hitstop or slowdown still comes out when the fighter acts again, as in SF6 and Strive.
///   Frames held by a global freeze still age them, as before: the Roman Cancel module relies on its freeze using up
///   the button press that started it.
/// Counters set later in the frame (hits, requests applied after the state machines) start counting next frame.
/// </remarks>
public sealed class TimeControl
{
    public FrameActivity BeginFrame(ref MatchState state)
    {
        bool isFrozen = state.Freeze.FramesLeft > 0;
        int advancing = 0;
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            int bit = 1 << fighter;
            bool isRunning = !isFrozen || (state.Freeze.ExemptFighters & bit) != 0;
            if (!isRunning)
            {
                continue;
            }

            if (RunOwnClock(ref state.Fighters[fighter].Timing))
            {
                advancing |= bit;
            }
            else
            {
                state.Inputs[fighter].MarkNewestSkipped();
            }
        }

        if (isFrozen && --state.Freeze.FramesLeft == 0)
        {
            state.Freeze.ExemptFighters = 0;
        }

        return new FrameActivity(advancing, isFrozen);
    }

    // True when the fighter advances: it got a frame of its own and that frame isn't spent in hitstop.
    private static bool RunOwnClock(ref FighterTiming timing)
    {
        if (!TickTimeScale(ref timing))
        {
            return false;
        }

        if (timing.HitstopFramesLeft > 0)
        {
            timing.HitstopFramesLeft--;
            return false;
        }

        return true;
    }

    // True when this match frame gives the fighter a frame of its own.
    private static bool TickTimeScale(ref FighterTiming timing)
    {
        if (timing.TimeScaleFramesLeft <= 0)
        {
            return true;
        }

        timing.TimeScaleProgress += timing.TimeScaleNumerator;
        bool hasOwnFrame = timing.TimeScaleProgress >= timing.TimeScaleDenominator;
        if (hasOwnFrame)
        {
            timing.TimeScaleProgress -= timing.TimeScaleDenominator;
        }

        if (--timing.TimeScaleFramesLeft == 0)
        {
            timing = timing with
            {
                TimeScaleNumerator = 0,
                TimeScaleDenominator = 0,
                TimeScaleProgress = 0,
            };
        }

        return hasOwnFrame;
    }
}
