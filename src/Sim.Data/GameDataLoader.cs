using System.Collections.Generic;
using Sim.Data.Characters;
using Sim.Data.Characters.Compiled;
using Sim.Data.Characters.Reading;
using Sim.Data.Hashing;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace Sim.Data;

/// <summary>Loads and validates every data file under the data root and computes the data hash.</summary>
/// <remarks>
/// New kinds of data (button set, motions, match rules) add their loader here and append to the same hash, in a
/// fixed order.
/// </remarks>
public sealed class GameDataLoader
{
    private readonly IDataSource _source;
    private readonly IButtonLookup _buttons;

    public GameDataLoader(IDataSource source, IButtonLookup buttons)
    {
        _source = source;
        _buttons = buttons;
    }

    /// <exception cref="DataValidationException">Any file is invalid; lists every problem in every file.</exception>
    public GameData Load()
    {
        DataErrors errors = new();
        List<CharacterData> characters = LoadCharacters(errors);
        errors.ThrowIfAny();

        using DataHashBuilder hash = new();
        hash.Add(characters.Count);
        foreach (CharacterData character in characters)
        {
            CharacterDataHash.Append(hash, character);
        }

        return new GameData(characters, hash.Finish());
    }

    // The data source lists folders in ordinal order, so characters (and the hash) never depend on the file system.
    private List<CharacterData> LoadCharacters(DataErrors errors)
    {
        CharacterLoader loader = new(_source, _buttons);
        List<CharacterData> characters = [];
        foreach (string folder in _source.ListDirectories(CharacterSchema.CharactersDirectory))
        {
            CharacterData? character = loader.Load(folder, errors);
            if (character is not null)
            {
                characters.Add(character);
            }
        }

        return characters;
    }
}
