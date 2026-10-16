using System.Collections.Generic;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Tests.Pipeline;

/// <summary>Test stage that records, per frame, which fighters advanced and whether the match was frozen.</summary>
internal sealed class AdvanceCountingStage : ITickStage
{
    public List<(bool Player1, bool Player2, bool IsFrozen)> Frames { get; } = [];

    public int AdvancedFrames(int fighter)
    {
        int count = 0;
        foreach ((bool player1, bool player2, _) in Frames)
        {
            count += (fighter == 0 ? player1 : player2) ? 1 : 0;
        }

        return count;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        Frames.Add((activity.Advances(0), activity.Advances(1), activity.IsFrozen));
    }
}
