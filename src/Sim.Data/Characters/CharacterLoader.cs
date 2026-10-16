using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Characters.Reading;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace Sim.Data.Characters;

/// <summary>Loads one character folder: character file, every listed move file, cross-checks, compact form.</summary>
public sealed class CharacterLoader
{
    private const string JsonExtension = ".json";

    private readonly IDataSource _source;
    private readonly IButtonLookup _buttons;

    public CharacterLoader(IDataSource source, IButtonLookup buttons)
    {
        _source = source;
        _buttons = buttons;
    }

    /// <summary>The character in compact form.</summary>
    /// <exception cref="DataValidationException">The data is invalid; lists every problem found.</exception>
    public CharacterData Load(string characterId)
    {
        DataErrors errors = new();
        CharacterData? character = Load(characterId, errors);
        errors.ThrowIfAny();
        return character!;
    }

    /// <summary>The character, or null when anything was reported to <paramref name="errors"/>.</summary>
    public CharacterData? Load(string characterId, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        CharacterDefinition? definition = CharacterFileReader.Read(_source, characterId, errors);
        if (definition is null)
        {
            return null;
        }

        MoveFileReader moveReader = new(_source, definition.MoveIds, _buttons, errors);
        List<MoveRecord> moves = [];
        foreach (string moveId in definition.MoveIds)
        {
            // Invalid list entries ("") were already reported by the character file reader.
            MoveRecord? move = moveId.Length == 0 ? null : moveReader.Read(characterId, moveId);
            if (move is not null)
            {
                moves.Add(move);
            }
        }

        ReportUnlistedMoveFiles(definition, errors);
        if (errors.Count > errorsBefore)
        {
            return null;
        }

        ReportNonBasicStateMoves(definition, moves, errors);
        return errors.Count > errorsBefore ? null : CharacterAssembler.Assemble(definition, moves);
    }

    // A move file that the move list forgot would silently be missing from the game.
    private void ReportUnlistedMoveFiles(CharacterDefinition definition, DataErrors errors)
    {
        string movesDirectory = CharacterSchema.MovesDirectory(definition.Id);
        foreach (string fileName in _source.ListFiles(movesDirectory))
        {
            if (!fileName.EndsWith(JsonExtension, StringComparison.Ordinal))
            {
                continue;
            }

            string moveId = fileName[..^JsonExtension.Length];
            if (!Contains(definition.MoveIds, moveId))
            {
                errors.Add($"{movesDirectory}/{fileName}", "",
                    $"move file is not in the \"moves\" list of {CharacterSchema.CharacterFile(definition.Id)}");
            }
        }
    }

    private static void ReportNonBasicStateMoves(
        CharacterDefinition definition, IReadOnlyList<MoveRecord> moves, DataErrors errors)
    {
        foreach ((string name, BasicState state) in CharacterJsonNames.BasicStates)
        {
            int moveIndex = definition.BasicStateMoves[(int)state];
            if (moveIndex != CharacterData.NoMove && moves[moveIndex].Move.Category != MoveCategory.Basic)
            {
                errors.Add(CharacterSchema.CharacterFile(definition.Id), $"states.{name}",
                    $"move '{definition.MoveIds[moveIndex]}' must have category \"basic\"");
            }
        }
    }

    private static bool Contains(IReadOnlyList<string> moveIds, string moveId)
    {
        foreach (string listed in moveIds)
        {
            if (string.Equals(listed, moveId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
