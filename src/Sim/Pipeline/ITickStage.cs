using Sim.State;

namespace Sim.Pipeline;

/// <summary>One step of the fixed tick order (ARCHITECTURE.md §4), run once per frame by the pipeline.</summary>
/// <remarks>
/// Symmetry rule: a stage computes both fighters from the state as it was when the stage started and writes the
/// results together, so no result depends on which player is processed first.
/// </remarks>
public interface ITickStage
{
    void Run(ref MatchState state, in FrameActivity activity);
}
