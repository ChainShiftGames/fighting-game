using System;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>The players of a match: the game is 1v1 (ARCHITECTURE.md §1), so the count is a fixed rule, not a setting.</summary>
internal static class MatchPlayers
{
    public const int Count = 2;

    public static void EnsureInputCount(ReadOnlySpan<InputWord> inputs, int expected)
    {
        if (inputs.Length != expected)
        {
            throw new ArgumentException($"Expected {expected} input words, got {inputs.Length}.", nameof(inputs));
        }
    }
}
