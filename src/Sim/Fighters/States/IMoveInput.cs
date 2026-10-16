using Sim.Characters;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Answers whether the inputs for moves and dashes were entered; the state machine never parses input.</summary>
public interface IMoveInput
{
    /// <summary>The move's motion and button were input for this frame.</summary>
    bool IsTriggered(in InputHistory history, in MoveData move, int facing);

    bool IsDashTriggered(in InputHistory history, int facing);

    bool IsBackdashTriggered(in InputHistory history, int facing);
}
