using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Characters;
using Sim.Data.Characters.Reading;
using Sim.Data.Hashing;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;

namespace Sim.Data;

/// <summary>Loads and validates every data file under the data root and computes the data hash.</summary>
/// <remarks>
/// New kinds of data (motions, match rules) add their loader here and append to the same hash, in a
/// fixed order.
/// </remarks>
public sealed class GameDataLoader
{
    private readonly IDataSource _source;

    public GameDataLoader(IDataSource source)
    {
        _source = source;
    }

    /// <exception cref="DataValidationException">Any file is invalid; lists every problem in every file.</exception>
    public GameData Load()
    {
        DataErrors errors = new();
        // Moves name their buttons, so without a valid button set every move would report a button error too.
        ButtonSet? buttons = ButtonSetLoader.Load(_source, errors);
        errors.ThrowIfAny();
        List<CharacterData> characters = LoadCharacters(new ButtonSetLookup(buttons!), errors);
        errors.ThrowIfAny();

        using DataHashBuilder hash = new();
        ButtonSetDataHash.Append(hash, buttons!);
        hash.Add(characters.Count);
        foreach (CharacterData character in characters)
        {
            CharacterDataHash.Append(hash, character);
        }

        return new GameData(buttons!, characters, hash.Finish());
    }

    // The data source lists folders in ordinal order, so characters (and the hash) never depend on the file system.
    private List<CharacterData> LoadCharacters(IButtonLookup buttons, DataErrors errors)
    {
        CharacterLoader loader = new(_source, buttons);
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
