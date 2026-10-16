using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Hashing;

namespace Sim.Data;

/// <summary>All loaded game data in compact form, plus its hash.</summary>
public sealed class GameData
{
    public GameData(IReadOnlyList<CharacterData> characters, DataHash hash)
    {
        Characters = characters;
        Hash = hash;
    }

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
