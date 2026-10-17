using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sim.Data.Characters.Reading;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// One character open in the editor: its <c>character.json</c> and every move file its move list names, as
/// editable JSON trees. The move list is read from the character file each time, so it can't go out of sync.
/// </summary>
public sealed class CharacterDocument
{
    private const string MovesField = "moves";

    private readonly Dictionary<string, EditedFile> _moves = new(StringComparer.Ordinal);
    private readonly List<EditedFile> _moveFiles = [];

    private CharacterDocument(string id, EditedFile character)
    {
        Id = id;
        Character = character;
    }

    /// <summary>The character's folder name under <c>data/characters/</c>.</summary>
    public string Id { get; }

    public EditedFile Character { get; }

    /// <summary>The move list in input priority order; an entry that isn't a string reads as "".</summary>
    public IReadOnlyList<string> MoveIds
    {
        get
        {
            List<string> ids = [];
            foreach (JsonNode? entry in MoveList() ?? [])
            {
                ids.Add(entry?.GetValueKind() == JsonValueKind.String ? entry.GetValue<string>() : "");
            }

            return ids;
        }
    }

    /// <summary>The character file followed by the move files, in the order they were opened.</summary>
    public IEnumerable<EditedFile> Files
    {
        get
        {
            yield return Character;
            foreach (EditedFile move in _moveFiles)
            {
                yield return move;
            }
        }
    }

    public bool HasChanges
    {
        get
        {
            foreach (EditedFile file in Files)
            {
                if (file.IsChanged)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Reads the character and its listed moves.</summary>
    /// <exception cref="DataValidationException">A file isn't a JSON object, or there is no character file.</exception>
    public static CharacterDocument Open(IDataSource source, string characterId)
    {
        DataErrors errors = new();
        string characterPath = CharacterSchema.CharacterFile(characterId);
        if (!IsFileName(characterId) || !source.FileExists(characterPath))
        {
            errors.Add(characterPath, "", "file not found");
            errors.ThrowIfAny();
        }

        JsonObject? characterRoot = JsonDataReader.Read(source, characterPath, errors);
        errors.ThrowIfAny();
        CharacterDocument document = new(characterId, new EditedFile(characterPath, characterRoot!));
        document.ReadMoves(source, errors);
        errors.ThrowIfAny();
        return document;
    }

    /// <summary>The move's file; null when the list names a move without a file (validation reports it).</summary>
    public EditedFile? Move(string moveId) => _moves.GetValueOrDefault(moveId);

    /// <summary>Swaps two entries of the move list, which changes their input priority.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An index is outside the move list.</exception>
    /// <exception cref="InvalidOperationException">The character file has no move list.</exception>
    public void SwapMoves(int index, int otherIndex)
    {
        JsonArray list = MoveList()
            ?? throw new InvalidOperationException($"{Character.Path} has no \"{MovesField}\" list.");
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)list.Count, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)otherIndex, (uint)list.Count, nameof(otherIndex));
        if (index == otherIndex)
        {
            return;
        }

        JsonNode? first = list[index];
        JsonNode? second = list[otherIndex];
        // A node can only have one parent: detach both before putting them back.
        list[index] = null;
        list[otherIndex] = null;
        list[index] = second;
        list[otherIndex] = first;
        Character.MarkChanged();
    }

    private JsonArray? MoveList() => Character.Root[MovesField] as JsonArray;

    private void ReadMoves(IDataSource source, DataErrors errors)
    {
        foreach (string moveId in MoveIds)
        {
            // Validation reports bad ids, duplicates and missing files; here they're just not opened.
            if (!IsFileName(moveId) || _moves.ContainsKey(moveId)
                || !source.FileExists(CharacterSchema.MoveFile(Id, moveId)))
            {
                continue;
            }

            string path = CharacterSchema.MoveFile(Id, moveId);
            JsonObject? root = JsonDataReader.Read(source, path, errors);
            if (root is not null)
            {
                EditedFile move = new(path, root);
                _moves.Add(moveId, move);
                _moveFiles.Add(move);
            }
        }
    }

    // Keeps an id like "../x" from reaching the data source as a path.
    private static bool IsFileName(string id)
    {
        if (id.Length == 0)
        {
            return false;
        }

        foreach (char c in id)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }
}
