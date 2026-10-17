using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Characters;
using Sim.Data.Characters.Reading;
using Sim.Data.Hashing;
using Sim.Data.Input;
using Sim.Data.Modules;
using Sim.Data.Motions;
using Sim.Data.Rules;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;
using Sim.Modules;

namespace Sim.Data;

/// <summary>Loads and validates every data file under the data root and computes the data hash.</summary>
/// <remarks>
/// New kinds of data (match rules…) add their loader here and append to the same hash, in a fixed order:
/// buttons, motions, match rules, module lineup, characters.
/// </remarks>
public sealed class GameDataLoader
{
    private readonly IDataSource _source;
    private readonly ModuleCatalog _modules;

    /// <summary>Checks the module lineup in data against the registered modules (<see cref="RegisteredModules"/>).</summary>
    public GameDataLoader(IDataSource source)
        : this(source, RegisteredModules.Create())
    {
    }

    /// <param name="modules">The modules the lineup in data may name.</param>
    public GameDataLoader(IDataSource source, ModuleCatalog modules)
    {
        _source = source;
        _modules = modules;
    }

    /// <exception cref="DataValidationException">Any file is invalid; lists every problem in every file.</exception>
    public GameData Load()
    {
        DataErrors errors = new();
        // Moves name their buttons, so without a valid button set every move would report a button error too.
        ButtonSet? buttons = ButtonSetLoader.Load(_source, errors);
        errors.ThrowIfAny();
        ButtonSetLookup buttonLookup = new(buttons!);
        MotionFile? motions = MotionSetLoader.Load(_source, buttonLookup, errors);
        MatchRulesFile? rules = MatchRulesFileLoader.Load(_source, errors);
        ModuleLineupFile? modules = ModuleLineupLoader.Load(_source, _modules, errors);
        List<CharacterData> characters = LoadCharacters(buttonLookup, errors);
        errors.ThrowIfAny();
        CheckMoveMotions(characters, motions!, errors);
        MoveRoleCheck.Check(characters, _modules, modules!, errors);
        errors.ThrowIfAny();

        using DataHashBuilder hash = new();
        ButtonSetDataHash.Append(hash, buttons!);
        MotionSetDataHash.Append(hash, motions!);
        MatchRulesDataHash.Append(hash, rules!);
        ModuleLineupDataHash.Append(hash, modules!);
        hash.Add(characters.Count);
        foreach (CharacterData character in characters)
        {
            CharacterDataHash.Append(hash, character);
        }

        return new GameData(buttons!, motions!, rules!, modules!, characters, hash.Finish());
    }

    // A move whose motion the motion set doesn't define could never be input.
    private static void CheckMoveMotions(List<CharacterData> characters, MotionFile motions, DataErrors errors)
    {
        foreach (CharacterData character in characters)
        {
            for (int move = 0; move < character.Moves.Length; move++)
            {
                int motion = character.Moves[move].Motion;
                if (character.Moves[move].Category != MoveCategory.Basic && !motions.Motions.TryFind(motion, out _))
                {
                    errors.Add(
                        CharacterSchema.MoveFile(character.Id, character.MoveIds[move]),
                        "input.motion",
                        $"motion {motion} isn't in {MotionSetSchema.File}");
                }
            }
        }
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
