using Sim.Input;
using Sim.Pipeline;
using Sim.State;
using Xunit;

namespace Sim.Tests.Pipeline;

/// <summary>Frames a running fighter skips are marked in its input history, so they don't age its buffered input.</summary>
public sealed class TimeControlTests
{
    private const int Player1 = 0;
    private const int Player2 = 1;

    private readonly TimeControl _timeControl = new();

    [Fact]
    public void AHitstopFrameIsMarkedSkippedForThatFighterOnly()
    {
        MatchState state = default;
        state.Fighters[Player1].Timing.HitstopFramesLeft = 1;

        FrameActivity activity = Frame(ref state);

        Assert.False(activity.Advances(Player1));
        Assert.True(state.Inputs[Player1].IsSkipped(0));
        Assert.False(state.Inputs[Player2].IsSkipped(0));
    }

    [Fact]
    public void ASlowedFighterIsMarkedSkippedOnTheFramesItGetsNoOwnFrame()
    {
        MatchState state = default;
        state.Fighters[Player2].Timing = new FighterTiming
        {
            TimeScaleNumerator = 1,
            TimeScaleDenominator = 2,
            TimeScaleFramesLeft = 4,
        };
        bool[] skipped = new bool[4];

        for (int frame = 0; frame < skipped.Length; frame++)
        {
            Frame(ref state);
            skipped[frame] = state.Inputs[Player2].IsSkipped(0);
        }

        Assert.Equal([true, false, true, false], skipped);
    }

    [Fact]
    public void AGlobalFreezeMarksTheFrozenFighterOnly()
    {
        // The press that started a freeze is used up instead (ADR-0033), so the freeze needn't age it.
        MatchState state = default;
        state.Freeze = new FreezeState { FramesLeft = 1, ExemptFighters = 1 << Player2 };

        Frame(ref state);

        Assert.True(state.Inputs[Player1].IsSkipped(0));
        Assert.False(state.Inputs[Player2].IsSkipped(0));
    }

    [Fact]
    public void AnAdvancingFighterIsNotMarked()
    {
        MatchState state = default;

        FrameActivity activity = Frame(ref state);

        Assert.True(activity.Advances(Player1));
        Assert.False(state.Inputs[Player1].IsSkipped(0));
        Assert.False(state.Inputs[Player2].IsSkipped(0));
    }

    // As the pipeline does: input is recorded first, then time control decides who advances.
    private FrameActivity Frame(ref MatchState state)
    {
        state.Inputs[Player1].Record(InputWord.Neutral);
        state.Inputs[Player2].Record(InputWord.Neutral);
        return _timeControl.BeginFrame(ref state);
    }
}
