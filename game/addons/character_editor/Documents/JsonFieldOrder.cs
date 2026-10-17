using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Sets an object's field and puts a new one where the schema lists it, so a field added in the editor lands where a
/// person would have written it and saved files keep the same field order.
/// </summary>
public static class JsonFieldOrder
{
    /// <summary>Replaces the field in place, or inserts it before the first later field in <paramref name="order"/>.</summary>
    /// <param name="order">Field names in schema order; a field not in it goes last.</param>
    public static void Set(JsonObject target, string field, JsonNode? value, IReadOnlyList<string> order)
    {
        if (target.ContainsKey(field))
        {
            target[field] = value;
            return;
        }

        int rank = IndexOf(order, field);
        if (rank >= 0)
        {
            for (int i = 0; i < target.Count; i++)
            {
                int otherRank = IndexOf(order, target.GetAt(i).Key);
                if (otherRank > rank)
                {
                    target.Insert(i, field, value);
                    return;
                }
            }
        }

        target.Add(field, value);
    }

    private static int IndexOf(IReadOnlyList<string> order, string field)
    {
        for (int i = 0; i < order.Count; i++)
        {
            if (order[i] == field)
            {
                return i;
            }
        }

        return -1;
    }
}
