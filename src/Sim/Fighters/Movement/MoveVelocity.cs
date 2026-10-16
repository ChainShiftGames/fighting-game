using System;
using Sim.Characters;

namespace Sim.Fighters.Movement;

/// <summary>Finds the velocity a move's data gives on one of its frames, if any.</summary>
public static class MoveVelocity
{
    public static bool TryFind(CharacterData character, in MoveData move, int frame, out VelocityData velocity)
    {
        // Ranges are few and in frame order (validated at load), so a linear scan is cheap and allocates nothing.
        ReadOnlySpan<VelocityData> velocities = character.VelocitiesOf(in move);
        foreach (VelocityData candidate in velocities)
        {
            if (frame >= candidate.FirstFrame && frame <= candidate.LastFrame)
            {
                velocity = candidate;
                return true;
            }
        }

        velocity = default;
        return false;
    }
}
