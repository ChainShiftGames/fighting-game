using Sim.State;

namespace Sim.Pipeline;

/// <summary>Several systems run as one tick step, in the given order (e.g. pushboxes and walls, then the camera).</summary>
/// <remarks>Lets one step of the fixed tick order hold more than one system without the pipeline changing.</remarks>
public sealed class StageSequence : ITickStage
{
    private readonly ITickStage[] _stages;

    public StageSequence(params ITickStage[] stages)
    {
        _stages = stages;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        foreach (ITickStage stage in _stages)
        {
            stage.Run(ref state, in activity);
        }
    }
}
