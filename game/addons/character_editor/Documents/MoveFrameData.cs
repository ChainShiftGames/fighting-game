using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Changes a move's frame data, optionally moving every frame range in the move along with its phase
/// (<see cref="FrameRemap"/>), so lengthening startup doesn't leave the hit boxes on the old active frames.
/// </summary>
public static class MoveFrameData
{
    /// <summary>The fields with a <c>firstFrame</c>/<c>lastFrame</c> range per item.</summary>
    private static readonly string[] _rangeLists = [MoveFields.Boxes, MoveFields.Cancels, MoveFields.Velocity];

    /// <summary>Writes <paramref name="phases"/> to the move.</summary>
    /// <param name="moveRanges">
    /// Also move boxes, cancel windows, velocity, invulnerability and projectile spawns with their phases. Only
    /// when the move had readable frame data of the same kind (basic or attack); otherwise ranges are left alone.
    /// </param>
    /// <param name="fieldOrder">The move's field names in schema order, for a frame data field that is missing.</param>
    public static void Set(JsonObject move, FramePhases phases, bool moveRanges, IReadOnlyList<string> fieldOrder)
    {
        FramePhases? old = FramePhases.Of(move);
        if (phases.IsBasic)
        {
            JsonFieldOrder.Set(move, MoveFields.Duration, phases.Duration, fieldOrder);
        }
        else
        {
            if (move[MoveFields.FrameData] is not JsonObject frameData)
            {
                frameData = [];
                JsonFieldOrder.Set(move, MoveFields.FrameData, frameData, fieldOrder);
            }

            frameData[MoveFields.Startup] = phases.Startup;
            frameData[MoveFields.Active] = phases.Active;
            frameData[MoveFields.Recovery] = phases.Recovery;
        }

        if (moveRanges && old is not null && old.IsBasic == phases.IsBasic && old != phases)
        {
            MoveRanges(move, new FrameRemap(old, phases));
        }
    }

    private static void MoveRanges(JsonObject move, FrameRemap remap)
    {
        foreach (string list in _rangeLists)
        {
            foreach (JsonNode? item in move[list] as JsonArray ?? [])
            {
                MoveRange(item as JsonObject, remap);
            }
        }

        MoveRange(move[MoveFields.Invulnerable] as JsonObject, remap);
        foreach (JsonNode? projectile in move[MoveFields.Projectiles] as JsonArray ?? [])
        {
            // Only the spawn: a projectile's own boxes count frames from its spawn, not from the move.
            if (projectile is JsonObject obj && JsonNodeValue.Int(obj[MoveFields.SpawnFrame]) is int spawn)
            {
                obj[MoveFields.SpawnFrame] = remap.First(spawn);
            }
        }
    }

    private static void MoveRange(JsonObject? range, FrameRemap remap)
    {
        if (range is null)
        {
            return;
        }

        if (JsonNodeValue.Int(range[MoveFields.FirstFrame]) is int first)
        {
            range[MoveFields.FirstFrame] = remap.First(first);
        }

        if (JsonNodeValue.Int(range[MoveFields.LastFrame]) is int last)
        {
            range[MoveFields.LastFrame] = remap.Last(last);
        }
    }
}
