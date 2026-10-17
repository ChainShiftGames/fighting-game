#if TOOLS
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;
using Sim.Data.Characters.Reading;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The character editor's main screen: opens a character from <c>data/characters/</c>, lists its moves and saves
/// the changed files through <see cref="CharacterSaver"/>, so only data the game accepts is written.
/// </summary>
/// <remarks>
/// Editor panels change the open <see cref="CharacterDocument"/>'s JSON in place, mark the file changed and call
/// <see cref="Refresh"/>; this screen owns opening, discarding and saving.
/// </remarks>
[Tool]
public partial class CharacterEditorScreen : VBoxContainer
{
    private readonly CharacterToolbar _toolbar = new();
    private readonly MoveList _moves = new();
    private readonly MoveView _move = new();
    private readonly ProblemList _problems = new();
    private readonly ConfirmationDialog _discard = new() { Title = "Unsaved changes", OkButtonText = "Discard" };
    private string? _dataRoot;
    private CharacterSaver? _saver;
    private CharacterDocument? _document;
    private string? _pendingCharacter;

    public CharacterEditorScreen()
    {
        SizeFlagsVertical = SizeFlags.ExpandFill;
        var split = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        split.AddChild(_moves);
        split.AddChild(_move);
        foreach (Node child in new Node[] { _toolbar, split, _problems, _discard })
        {
            AddChild(child);
        }

        _toolbar.CharacterChosen += id => OpenUnlessUnsaved(id);
        _toolbar.ReloadPressed += () => OpenUnlessUnsaved(_document?.Id);
        _toolbar.ValidatePressed += Validate;
        _toolbar.SavePressed += Save;
        _moves.MoveSelected += ShowMove;
        _moves.SwapRequested += SwapMoves;
        _discard.Confirmed += () => Open(_pendingCharacter);
    }

    /// <summary>The open character's id when it has unsaved changes; else null.</summary>
    public string? UnsavedCharacter => _document is { HasChanges: true } ? _document.Id : null;

    /// <param name="dataRoot">Full path of the <c>data/</c> folder; null when there is none (nothing to edit).</param>
    public void Build(string? dataRoot)
    {
        _dataRoot = dataRoot;
        if (dataRoot is null)
        {
            _toolbar.Disable("No data/ folder next to game/: nothing to edit.");
            return;
        }

        _saver = new CharacterSaver(dataRoot);
        IReadOnlyList<string> ids =
            new DirectoryDataSource(dataRoot).ListDirectories(CharacterSchema.CharactersDirectory);
        _toolbar.ShowCharacters(ids, null);
        Open(ids.Count > 0 ? ids[0] : null);
    }

    /// <summary>Shows the open document again after a panel changed it.</summary>
    public void Refresh(string status)
    {
        _moves.ShowMoves(_document, _moves.SelectedIndex);
        ShowMove(_moves.SelectedIndex);
        _toolbar.ShowState(_document is not null, _document?.HasChanges ?? false, status);
    }

    private void OpenUnlessUnsaved(string? characterId)
    {
        if (_document is { HasChanges: true })
        {
            _pendingCharacter = characterId;
            _toolbar.Select(_document.Id);
            _discard.DialogText = $"Discard the unsaved changes to '{_document.Id}'?";
            _discard.PopupCentered();
            return;
        }

        Open(characterId);
    }

    private void Open(string? characterId)
    {
        _document = null;
        _problems.ShowProblems([]);
        string status = "No characters in data/characters/";
        if (characterId is not null && _dataRoot is not null)
        {
            try
            {
                _document = CharacterDocument.Open(new DirectoryDataSource(_dataRoot), characterId);
                status = $"Opened {characterId}";
                // Show problems already on disk (e.g. from a hand edit) straight away.
                _problems.ShowProblems(_saver!.Validate(_document));
            }
            catch (DataValidationException exception)
            {
                status = $"Can't open {characterId}";
                _problems.ShowProblems(exception.Errors);
            }
        }

        _toolbar.Select(characterId);
        _moves.ShowMoves(_document, 0);
        Refresh(status);
    }

    private void ShowMove(int index)
    {
        IReadOnlyList<string> moveIds = _document?.MoveIds ?? [];
        _move.ShowMove(index >= 0 && index < moveIds.Count ? _document!.Move(moveIds[index]) : null);
    }

    private void SwapMoves(int index, int otherIndex)
    {
        _document!.SwapMoves(index, otherIndex);
        _moves.ShowMoves(_document, otherIndex);
        Refresh("Move order changed");
    }

    private void Validate()
    {
        IReadOnlyList<DataError> problems = _saver!.Validate(_document!);
        _problems.ShowProblems(problems);
        Refresh(problems.Count == 0 ? "Valid" : "Invalid: see the problems below");
    }

    private void Save()
    {
        IReadOnlyList<DataError> problems = _saver!.Save(_document!);
        _problems.ShowProblems(problems);
        Refresh(problems.Count == 0 ? $"Saved {_document!.Id}" : "Not saved: fix the problems below");
    }
}
#endif
