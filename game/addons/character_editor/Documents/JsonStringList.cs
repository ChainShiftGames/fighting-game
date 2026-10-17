using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A field holding a list of distinct strings (a cancel's <c>on</c> or <c>into</c>, a move's <c>buttons</c>), edited
/// one member at a time. An optional list that becomes empty is removed, so the file doesn't gain empty fields.
/// </summary>
public static class JsonStringList
{
    /// <summary>The list's strings in order; entries that aren't strings are skipped. Empty when there is no list.</summary>
    public static IReadOnlyList<string> Read(JsonObject target, string field)
    {
        List<string> members = [];
        foreach (JsonNode? entry in target[field] as JsonArray ?? [])
        {
            if (JsonNodeValue.Text(entry) is string text)
            {
                members.Add(text);
            }
        }

        return members;
    }

    /// <summary>
    /// The members to offer: <paramref name="choices"/>, then any the list holds that aren't among them (a removed flag,
    /// a deleted move), so they can still be unticked; validation reports them.
    /// </summary>
    public static IReadOnlyList<string> Offered(IReadOnlyList<string> choices, JsonObject target, string field)
    {
        List<string> offered = [.. choices];
        foreach (string member in Read(target, field))
        {
            if (!offered.Contains(member))
            {
                offered.Add(member);
            }
        }

        return offered;
    }

    public static bool Contains(JsonObject target, string field, string member)
    {
        foreach (string existing in Read(target, field))
        {
            if (existing == member)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Adds <paramref name="member"/> at the end, or removes every copy of it.</summary>
    /// <param name="keepWhenEmpty">The schema requires the field, so an empty list stays (validation reports it).</param>
    /// <param name="order">Field names in schema order, for a list that is missing.</param>
    public static void Set(
        JsonObject target, string field, string member, bool included, bool keepWhenEmpty, IReadOnlyList<string> order)
    {
        if (target[field] is not JsonArray list)
        {
            if (!included)
            {
                return;
            }

            list = [];
            JsonFieldOrder.Set(target, field, list, order);
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (JsonNodeValue.Text(list[i]) == member)
            {
                list.RemoveAt(i);
            }
        }

        if (included)
        {
            list.Add(member);
        }
        else if (list.Count == 0 && !keepWhenEmpty)
        {
            target.Remove(field);
        }
    }
}
