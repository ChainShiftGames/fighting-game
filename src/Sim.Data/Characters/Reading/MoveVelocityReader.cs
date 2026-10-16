using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads a move's <c>velocity</c> ranges: frames and speed, in frame order without overlaps.</summary>
/// <remarks>Ordered ranges that don't overlap give every frame at most one velocity.</remarks>
public static class MoveVelocityReader
{
    public static IReadOnlyList<VelocityData> Read(FieldReader move, MoveFrames frames)
    {
        List<VelocityData> velocities = [];
        IReadOnlyList<FieldReader?> entries = move.ObjectArray("velocity", required: false);
        for (int i = 0; i < entries.Count; i++)
        {
            FieldReader? entry = entries[i];
            if (entry is null)
            {
                continue;
            }

            VelocityData velocity = ReadVelocity(entry, frames);
            if (velocities.Count > 0 && velocity.FirstFrame <= velocities[^1].LastFrame)
            {
                int previousLast = velocities[^1].LastFrame;
                move.Error($"velocity[{i}]", "velocity ranges must be in frame order and must not overlap; "
                    + $"this one starts on frame {velocity.FirstFrame}, the one before ends on {previousLast}");
            }

            velocities.Add(velocity);
        }

        return velocities;
    }

    private static VelocityData ReadVelocity(FieldReader entry, MoveFrames frames)
    {
        const int MaxDistance = CharacterSchema.MaxDistance;
        int firstFrame = entry.RequiredInt("firstFrame", 1, frames.Total);
        VelocityData velocity = new()
        {
            FirstFrame = firstFrame,
            LastFrame = entry.RequiredInt("lastFrame", firstFrame, frames.Total),
            X = entry.RequiredInt("x", -MaxDistance, MaxDistance),
            Y = entry.OptionalInt("y", 0, -MaxDistance, MaxDistance),
        };
        entry.Finish();
        return velocity;
    }
}
