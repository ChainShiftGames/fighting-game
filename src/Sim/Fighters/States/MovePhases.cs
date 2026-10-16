using Sim.Characters;

namespace Sim.Fighters.States;

/// <summary>Works out a move's phase on a given frame from its frame data.</summary>
public static class MovePhases
{
    public static MovePhase Of(in MoveData move, int frame)
    {
        if (move.FirstActiveFrame == 0)
        {
            return MovePhase.None;
        }

        if (frame < move.FirstActiveFrame)
        {
            return MovePhase.Startup;
        }

        return frame <= move.LastActiveFrame ? MovePhase.Active : MovePhase.Recovery;
    }
}
