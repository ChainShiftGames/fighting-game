using Sim.State;

namespace Sim.Pipeline;

/// <summary>A tick step whose system isn't built yet: it changes nothing, so the full order already runs.</summary>
public sealed class IdleStage : ITickStage
{
    public void Run(ref MatchState state, in FrameActivity activity)
    {
    }
}
