using System;
using System.Collections.Generic;

namespace Sim.Fighters;

/// <summary>The game-wide fighter flag names in bit order (<c>data/rules/flags.json</c>, ADR-0023).</summary>
/// <remarks>Data names flags; the logic uses their bits (<c>FighterState.Flags</c>), resolved once at load or match start.</remarks>
public sealed class FighterFlagNames
{
    /// <summary>Flags fit one int.</summary>
    public const int MaxFlags = 32;

    private readonly string[] _names;

    /// <exception cref="ArgumentException">More than <see cref="MaxFlags"/> names, or one twice.</exception>
    public FighterFlagNames(IReadOnlyList<string> names)
    {
        if (names.Count > MaxFlags)
        {
            throw new ArgumentException($"{names.Count} fighter flags don't fit; at most {MaxFlags}.", nameof(names));
        }

        _names = new string[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            if (Array.IndexOf(_names, names[i], 0, i) >= 0)
            {
                throw new ArgumentException($"Fighter flag '{names[i]}' is named twice.", nameof(names));
            }

            _names[i] = names[i];
        }
    }

    /// <summary>No flags.</summary>
    public static FighterFlagNames None { get; } = new([]);

    public IReadOnlyList<string> Names => _names;

    /// <summary>The flag's bit as a mask; 0 when no flag has that name.</summary>
    public int MaskOf(string name)
    {
        int index = Array.IndexOf(_names, name);
        return index < 0 ? 0 : 1 << index;
    }
}
