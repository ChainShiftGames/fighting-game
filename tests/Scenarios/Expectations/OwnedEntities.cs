using System;
using System.Globalization;
using Sim.State;

namespace Scenarios.Tests.Expectations;

/// <summary>The shared part of the entity kinds: a fighter's live entities in the pool (P1-21).</summary>
public static class OwnedEntities
{
    /// <summary>What the single-entity kinds read when the fighter has no entity out.</summary>
    public const int None = int.MinValue;

    public static int Count(in MatchState state, int owner)
    {
        int count = 0;
        for (int slot = EntityPool.FirstSlotOf(owner); slot < EntityPool.FirstSlotOf(owner + 1); slot++)
        {
            count += state.Entities.Slots[slot].IsActive;
        }

        return count;
    }

    /// <summary>A value of the fighter's entity in its lowest slot (its oldest), or <see cref="None"/>.</summary>
    public static int ReadFirst(in MatchState state, int owner, Func<EntityState, int> value)
    {
        for (int slot = EntityPool.FirstSlotOf(owner); slot < EntityPool.FirstSlotOf(owner + 1); slot++)
        {
            if (state.Entities.Slots[slot].IsActive != 0)
            {
                return value(state.Entities.Slots[slot]);
            }
        }

        return None;
    }

    /// <summary>An integer, or <c>none</c> for no entity.</summary>
    /// <exception cref="FormatException">Neither.</exception>
    public static int Parse(string text, string keyword) =>
        text == "none" ? None
        : int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value) ? value
        : throw new FormatException($"'{text}' isn't an integer or 'none' ('{keyword}')");

    public static string Format(int value) =>
        value == None ? "none" : value.ToString(CultureInfo.InvariantCulture);
}
