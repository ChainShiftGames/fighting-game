using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// How each of a move's hits must be blocked (its <c>guard</c>: mid, low, overhead or unblockable), its own hits
/// first and then its projectiles'. Read from and written to the file in place; validation checks the values.
/// </summary>
public static class MoveGuards
{
    private const string GuardField = "guard";
    private const string HitField = "hit";

    /// <summary>One hit that has a guard: what to call it in the editor and the hit object that holds it.</summary>
    public sealed record Slot(string Label, JsonObject Hit)
    {
        /// <summary>The guard the file gives it; null when it has none (validation reports that).</summary>
        public string? Guard => JsonNodeValue.Text(Hit[GuardField]);
    }

    public static IReadOnlyList<Slot> Of(JsonObject move)
    {
        List<Slot> slots = [];
        List<JsonObject> hits = Objects(move[MoveFields.Hits]);
        for (int i = 0; i < hits.Count; i++)
        {
            slots.Add(new Slot(hits.Count == 1 ? "Hit" : $"Hit {i + 1}", hits[i]));
        }

        List<JsonObject> projectiles = Objects(move[MoveFields.Projectiles]);
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i][HitField] is JsonObject hit)
            {
                slots.Add(new Slot(projectiles.Count == 1 ? "Projectile" : $"Projectile {i + 1}", hit));
            }
        }

        return slots;
    }

    /// <summary>The move's guards, each once, in hit order (e.g. ["mid", "overhead"]); empty when it has none.</summary>
    public static IReadOnlyList<string> Distinct(JsonObject move)
    {
        List<string> guards = [];
        foreach (Slot slot in Of(move))
        {
            if (slot.Guard is string guard && !guards.Contains(guard))
            {
                guards.Add(guard);
            }
        }

        return guards;
    }

    public static void Set(Slot slot, string guard) => slot.Hit[GuardField] = guard;

    private static List<JsonObject> Objects(JsonNode? list)
    {
        List<JsonObject> objects = [];
        foreach (JsonNode? item in list as JsonArray ?? [])
        {
            if (item is JsonObject obj)
            {
                objects.Add(obj);
            }
        }

        return objects;
    }
}
