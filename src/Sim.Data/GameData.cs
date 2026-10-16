using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Hashing;
using Sim.Input;

namespace Sim.Data;

/// <summary>All loaded game data in compact form, plus its hash.</summary>
public sealed class GameData
{
    public GameData(ButtonSet buttons, IReadOnlyList<CharacterData> characters, DataHash hash)
    {
        Buttons = buttons;
        Characters = characters;
        Hash = hash;
    }

    /// <summary>The buttons of the input word (ADR-0010).</summary>
    public ButtonSet Buttons { get; }

    /// <summary>Sorted by id (ordinal), the same on every platform.</summary>
    public IReadOnlyList<CharacterData> Characters { get; }

    public DataHash Hash { get; }

    public CharacterData Character(string id)
    {
        foreach (CharacterData character in Characters)
        {
            if (string.Equals(character.Id, id, StringComparison.Ordinal))
            {
                return character;
            }
        }

        throw new KeyNotFoundException($"No character with id '{id}' was loaded.");
    }
}
