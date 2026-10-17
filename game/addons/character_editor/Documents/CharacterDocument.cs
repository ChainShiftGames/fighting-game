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
/// editable JSON trees, plus the move files to delete on saving. The move list is read from the character file each
/// time, and adding or removing a move changes list and files together, so they can't go out of sync.
/// </summary>
public sealed class CharacterDocument
{
    private const string MovesField = "moves";

    private readonly Dictionary<string, EditedFile> _moves = new(StringComparer.Ordinal);
    private readonly List<EditedFile> _moveFiles = [];
    // Paths of new files not yet on disk, and of files on disk to delete when saving.
    private readonly HashSet<string> _created = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _removed = new(StringComparer.Ordinal);

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

    /// <summary>The character file followed by the move files, in the order they were opened or added.</summary>
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

    /// <summary>Data-relative paths of the move files that saving deletes.</summary>
    public IReadOnlyCollection<string> RemovedFiles => _removed;

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

            return _removed.Count > 0;
        }
    }

    /// <summary>Reads the character and its listed moves.</summary>
    /// <exception cref="DataValidationException">A file isn't a JSON object, or there is no character file.</exception>
    public static CharacterDocument Open(IDataSource source, string characterId)
    {
        DataErrors errors = new();
        string characterPath = CharacterSchema.CharacterFile(characterId);
        if (!MoveIdCheck.IsFileName(characterId) || !source.FileExists(characterPath))
        {
            errors.Add(characterPath, "", "file not found");
            errors.ThrowIfAny();
        }

        EditedFile? character = JsonDataReader.Open(source, characterPath, errors);
        errors.ThrowIfAny();
        CharacterDocument document = new(characterId, character!);
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
        JsonArray list = RequiredMoveList();
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

    /// <summary>Adds a move file and lists it at <paramref name="listIndex"/> (its input priority).</summary>
    /// <exception cref="ArgumentException">The id can't name a new move (<see cref="MoveIdCheck.Problem"/>).</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is past the end of the move list.</exception>
    /// <exception cref="InvalidOperationException">The character file has no move list.</exception>
    public EditedFile AddMove(string moveId, JsonObject root, int listIndex)
    {
        JsonArray list = RequiredMoveList();
        if (MoveIdCheck.Problem(moveId, MoveIds) is string problem)
        {
            throw new ArgumentException(problem, nameof(moveId));
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)listIndex, (uint)list.Count, nameof(listIndex));
        string path = CharacterSchema.MoveFile(Id, moveId);
        // A file deleted earlier in this session is still on disk: saving overwrites it instead.
        if (!_removed.Remove(path))
        {
            _created.Add(path);
        }

        EditedFile move = new(path, root);
        move.MarkChanged();
        _moves.Add(moveId, move);
        _moveFiles.Add(move);
        list.Insert(listIndex, moveId);
        Character.MarkChanged();
        return move;
    }

    /// <summary>Removes every entry of the move from the move list; saving deletes its file.</summary>
    /// <exception cref="InvalidOperationException">The character file has no move list.</exception>
    public void RemoveMove(string moveId)
    {
        JsonArray list = RequiredMoveList();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i]?.GetValueKind() == JsonValueKind.String && list[i]!.GetValue<string>() == moveId)
            {
                list.RemoveAt(i);
            }
        }

        if (_moves.Remove(moveId, out EditedFile? move))
        {
            _moveFiles.Remove(move);
            if (!_created.Remove(move.Path))
            {
                _removed.Add(move.Path);
            }
        }

        Character.MarkChanged();
    }

    /// <summary>The files on disk now match the document: changed files written, removed files deleted.</summary>
    public void MarkSaved()
    {
        foreach (EditedFile file in Files)
        {
            file.MarkSaved();
        }

        _created.Clear();
        _removed.Clear();
    }

    private JsonArray? MoveList() => Character.Root[MovesField] as JsonArray;

    private JsonArray RequiredMoveList() => MoveList()
        ?? throw new InvalidOperationException($"{Character.Path} has no \"{MovesField}\" list.");

    private void ReadMoves(IDataSource source, DataErrors errors)
    {
        foreach (string moveId in MoveIds)
        {
            // Validation reports bad ids, duplicates and missing files; here they're just not opened.
            if (!MoveIdCheck.IsFileName(moveId) || _moves.ContainsKey(moveId)
                || !source.FileExists(CharacterSchema.MoveFile(Id, moveId)))
            {
                continue;
            }

            EditedFile? move = JsonDataReader.Open(source, CharacterSchema.MoveFile(Id, moveId), errors);
            if (move is not null)
            {
                _moves.Add(moveId, move);
                _moveFiles.Add(move);
            }
        }
    }
}
