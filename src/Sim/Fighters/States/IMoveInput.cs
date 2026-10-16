using Sim.Characters;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Answers whether moves, dashes and jumps were input; the state machine never parses input.</summary>
public interface IMoveInput
{
    /// <summary>The move's motion and button were input for this frame.</summary>
    bool IsTriggered(in InputHistory history, in MoveData move, int facing);

    /// <summary>A forward dash was input: a dash on the ground, an air dash in the air.</summary>
    bool IsDashTriggered(in InputHistory history, int facing);

    /// <summary>A backdash was input: a backdash on the ground, a backward air dash in the air.</summary>
    bool IsBackdashTriggered(in InputHistory history, int facing);

    /// <summary>The jump input starting a prejump this frame was a high jump (e.g. 2 then 8).</summary>
    bool IsHighJumpTriggered(in InputHistory history, int facing);

    /// <summary>A new jump input while airborne, for a double jump; holding up from the first jump isn't one.</summary>
    bool IsAirJumpTriggered(in InputHistory history, int facing);
}
