using Sim.Input;
using Sim.State;

namespace Sim.Pipeline;

/// <summary>The first tick step: turns both players' raw input words into the logic's input for this frame.</summary>
/// <remarks>Runs every frame, also during freeze and hitstop, so input is never lost (buffering, motions).</remarks>
public interface IInputStage
{
    void Run(ref MatchState state, InputWord player1, InputWord player2);
}
