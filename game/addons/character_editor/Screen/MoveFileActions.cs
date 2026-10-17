#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// Creating, duplicating and deleting moves of the open character, with the dialogs that ask for the new id or
/// confirm a delete (listing where the move is still named). Saving writes or removes the files.
/// </summary>
[Tool]
public partial class MoveFileActions : Node
{
    private readonly MoveIdDialog _moveId = new();
    private readonly ConfirmationDialog _delete = new() { Title = "Delete move", OkButtonText = "Delete" };
    private CharacterDocument? _document;
    private Action<string, string>? _pendingMoveId;
    private int _pendingDelete = -1;

    public MoveFileActions()
    {
        AddChild(_moveId);
        AddChild(_delete);
        _moveId.Accepted += (id, category) => _pendingMoveId?.Invoke(id, category);
        _delete.Confirmed += () => Delete(_pendingDelete);
    }

    /// <summary>The move list changed: a status line and the list position to select.</summary>
    public event Action<string, int>? Done;

    /// <summary>Asks for the id and category of a new move to list after <paramref name="afterIndex"/>.</summary>
    public void AskNew(CharacterDocument document, EditingContext context, int afterIndex)
    {
        _document = document;
        _pendingMoveId = (id, category) =>
            Add(id, MoveTemplate.New(id, category, context.MoveSchema, context.FirstButton), afterIndex + 1);
        IReadOnlyList<string> categories = context.MoveSchema.Field(MoveFields.Category)?.Choices ?? [];
        _moveId.Ask("New move", "", categories, document.MoveIds);
    }

    /// <summary>Asks for the id of a copy of the move at <paramref name="index"/>, listed right after it.</summary>
    public void AskDuplicate(CharacterDocument document, EditingContext context, int index)
    {
        _document = document;
        string sourceId = document.MoveIds[index];
        if (document.Move(sourceId) is not EditedFile source)
        {
            Done?.Invoke($"Can't duplicate {sourceId}: it has no file", index);
            return;
        }

        _pendingMoveId = (id, _) => Add(id, MoveTemplate.Copy(source.Root, id, context.MoveSchema), index + 1);
        _moveId.Ask($"Duplicate {sourceId}", sourceId + "_copy", [], document.MoveIds);
    }

    /// <summary>Asks to delete the move at <paramref name="index"/>, warning where it is still named.</summary>
    public void AskDelete(CharacterDocument document, int index)
    {
        _document = document;
        _pendingDelete = index;
        string id = document.MoveIds[index];
        IReadOnlyList<string> places = MoveReferences.Find(document, id);
        _delete.DialogText = places.Count == 0
            ? $"Delete '{id}'? Saving removes its file."
            : $"Delete '{id}'? Saving removes its file. It is still named in:\n  {string.Join("\n  ", places)}\n"
                + "Saving is refused until those are changed.";
        _delete.PopupCentered();
    }

    private void Add(string id, JsonObject root, int listIndex)
    {
        try
        {
            _document!.AddMove(id, root, listIndex);
        }
        catch (ArgumentException exception)
        {
            Done?.Invoke($"Can't add {id}: {exception.Message}", listIndex - 1);
            return;
        }

        Done?.Invoke($"Added {id}", listIndex);
    }

    private void Delete(int index)
    {
        string id = _document!.MoveIds[index];
        _document.RemoveMove(id);
        Done?.Invoke($"Deleted {id}", Math.Min(index, _document.MoveIds.Count - 1));
    }
}
#endif
