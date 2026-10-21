#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Drawer;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.CharacterEditor.Screen.Map;
using FightingGame.Presentation.World;
using Godot;
using Sim.Data.Characters.Reading;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The character editor's main screen, the move map: opens a character from <c>data/characters/</c>, shows its moves
/// as a map of cards and chain arrows, edits the selected move (or the character file) in the drawer, and saves the
/// changed files through <see cref="CharacterSaver"/>, so only data the game accepts is written.
/// </summary>
/// <remarks>
/// The drawer's pages change the open <see cref="CharacterDocument"/>'s JSON in place and raise an event; this
/// screen owns opening, discarding, adding, deleting and reordering moves, checking and saving.
/// </remarks>
[Tool]
public partial class CharacterEditorScreen : VBoxContainer
{
    private const float DrawerWidth = 340f;
    private const string SettingsPath = "res://addons/character_editor/move_map.json";

    private readonly MapTopBar _topBar = new();
    private readonly MoveMapCanvas _canvas = new();
    private readonly FrameStrip _strip = new();
    private readonly MoveDrawer _drawer = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly MoveFileActions _fileActions = new();
    private readonly ProblemPopup _problemPopup = new();
    private readonly ConfirmationDialog _discard = new() { Title = "Unsaved changes", OkButtonText = "Discard" };
    private readonly float _scale = EditorInterface.Singleton.GetEditorScale();
    private MoveMapSettings _settings = MoveMapSettings.Empty;
    private MapLens _lens = MapLens.Combos;
    private string? _dataRoot;
    private CharacterSaver? _saver;
    private CharacterDocument? _document;
    private ArtTimingFile? _art;
    private EditingContext? _context;
    private IReadOnlyList<DataError> _problems = [];
    private string? _selected;
    private string? _pendingCharacter;
    private string _status = "";
    private bool _checkQueued;

    public CharacterEditorScreen()
    {
        Theme = MapTheme.Build(_scale);
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 0);
        var mapSide = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        mapSide.AddThemeConstantOverride("separation", 0);
        mapSide.AddChild(_canvas);
        mapSide.AddChild(_strip);
        _drawer.CustomMinimumSize = new Vector2(DrawerWidth * _scale, 0f);
        var split = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        split.AddChild(mapSide);
        split.AddChild(_drawer);
        foreach (Node child in new Node[] { _topBar, split, _fileActions, _problemPopup, _discard })
        {
            AddChild(child);
        }

        _topBar.CharacterChosen += OpenUnlessUnsaved;
        _topBar.LensChosen += lens =>
        {
            _lens = lens;
            ShowMap(refit: true);
        };
        _topBar.ProblemsPressed += chip =>
        {
            _problemPopup.ShowProblems(_problems);
            _problemPopup.OpenUnder(chip, _scale);
        };
        _topBar.SavePressed += Save;
        _topBar.MenuPicked += OnMenu;
        _canvas.MoveSelected += Select;
        _canvas.MoveAction += OnCardMenu;
        _drawer.FrameShown += _strip.MarkFrame;
        _strip.FrameClicked += _drawer.SelectFrame;
        _drawer.Edited += OnEdited;
        _drawer.ArtEdited += status =>
        {
            _status = status;
            ShowState();
        };
        _fileActions.Done += (status, index) =>
        {
            IReadOnlyList<string> ids = _document!.MoveIds;
            _selected = index >= 0 && index < ids.Count ? ids[index] : null;
            _status = status;
            OnListChanged();
        };
        _discard.Confirmed += () => Open(_pendingCharacter);
    }

    /// <summary>The open character's id when it has unsaved changes; else null.</summary>
    public string? UnsavedCharacter => HasChanges ? _document!.Id : null;

    // The data files or the art file's animation timing.
    private bool HasChanges => _document is { HasChanges: true } || _art is { IsChanged: true };

    /// <param name="dataRoot">Full path of the <c>data/</c> folder; null when there is none (nothing to edit).</param>
    public void Build(string? dataRoot)
    {
        _dataRoot = dataRoot;
        if (dataRoot is null)
        {
            _topBar.Disable("No data/ folder next to game/: nothing to edit.");
            return;
        }

        _settings = MoveMapSettings.Parse(FileAccess.GetFileAsString(SettingsPath));
        _saver = new CharacterSaver(dataRoot);
        IReadOnlyList<string> ids =
            new DirectoryDataSource(dataRoot).ListDirectories(CharacterSchema.CharactersDirectory);
        _topBar.ShowCharacters(ids, null);
        // Deferred: the plugin adds this screen after Build, and drawn controls size themselves from its theme.
        string? first = ids.Count > 0 ? ids[0] : null;
        Callable.From(() => Open(first)).CallDeferred();
    }

    private void OpenUnlessUnsaved(string? characterId)
    {
        if (UnsavedCharacter is string open)
        {
            _pendingCharacter = characterId;
            _topBar.Select(open);
            _discard.DialogText = $"Discard the unsaved changes to '{open}'?";
            _discard.PopupCentered();
            return;
        }

        Open(characterId);
    }

    private void Open(string? characterId)
    {
        _document = null;
        _art = null;
        _selected = null;
        _problems = [];
        _status = "No characters in data/characters/";
        if (characterId is not null && _dataRoot is not null)
        {
            try
            {
                var source = new DirectoryDataSource(_dataRoot);
                _document = CharacterDocument.Open(source, characterId);
                _context = EditingContext.Read(source);
                _art = ArtTimingFile.Open(ProjectSettings.GlobalizePath(FighterArtFile.PathFor(characterId)));
                _status = $"Opened {characterId}";
                // Show problems already on disk (e.g. from a hand edit) straight away.
                _problems = _saver!.Validate(_document);
            }
            catch (DataValidationException exception)
            {
                _status = $"Can't open {characterId}";
                _problems = exception.Errors;
            }
        }

        _topBar.Select(characterId);
        _drawer.ShowCharacter(_document?.Id, _document?.MoveIds ?? []);
        ShowMap(refit: true);
    }

    // Lays the map out again from the document; keeps the selection when the lens still shows it.
    private void ShowMap(bool refit)
    {
        _problemPopup.ShowProblems(_problems);
        if (_document is null)
        {
            _canvas.Clear();
            ShowSelected(null);
            ShowState();
            return;
        }

        IReadOnlyList<ChainLink> links = MoveChains.Of(_document);
        MoveMapLayout layout = MoveMapLayout.Of(_document, _settings, _lens, links);
        var placement = new MapPlacement(layout, Categories(layout));
        if (_selected is null || layout.Find(_selected) is null)
        {
            _selected = FirstMove(layout);
            ShowSelected(_selected);
        }

        _canvas.ShowMap(placement, MapCards.Of(_document, layout, _settings, _problems), links, _selected, refit);
        ShowState();
    }

    private void Select(string moveId)
    {
        _selected = moveId;
        _canvas.Select(moveId);
        ShowSelected(moveId);
    }

    private void ShowSelected(string? moveId)
    {
        DrawerMove? move = moveId is not null && _document?.Move(moveId) is EditedFile file
            ? new DrawerMove(moveId, file, _document, _context!, _settings, _art)
            : null;
        _drawer.ShowMove(move);
        ShowStrip();
    }

    // Only the strip: showing the move in the drawer again would rebuild the page being edited.
    private void ShowStrip()
    {
        EditedFile? file = _selected is null ? null : _document?.Move(_selected);
        _strip.ShowMove(file is null ? null : MoveNames.Of(_selected!, file.Root, _settings), file?.Root);
        _strip.MarkFrame(_drawer.Frame);
    }

    // A drawer page changed a value in place; its controls are already up to date.
    private void OnEdited(bool cardsChanged)
    {
        _status = "Edited";
        if (cardsChanged)
        {
            ShowMap(refit: false);
            ShowStrip();
        }
        else
        {
            ShowState();
        }

        // One check after a burst of edits (e.g. holding a stepper), not one per value.
        if (!_checkQueued)
        {
            _checkQueued = true;
            Callable.From(CheckData).CallDeferred();
        }
    }

    private void CheckData()
    {
        _checkQueued = false;
        if (_document is not null)
        {
            _problems = _saver!.Validate(_document);
            ShowMap(refit: false);
        }
    }

    // Moves were added, removed or reordered: the map and the drawer's move lists follow.
    private void OnListChanged()
    {
        _drawer.ShowCharacter(_document!.Id, _document.MoveIds);
        _problems = _saver!.Validate(_document);
        ShowMap(refit: false);
        ShowSelected(_selected);
    }

    private void OnMenu(MapTopBar.MenuAction action)
    {
        if (_document is null)
        {
            return;
        }

        switch (action)
        {
            case MapTopBar.MenuAction.NewMove:
                _fileActions.AskNew(_document, _context!, SelectedIndex() ?? (_document.MoveIds.Count - 1));
                break;
            case MapTopBar.MenuAction.CharacterFile:
                _drawer.ShowCharacterFile(_document, _context!);
                break;
            case MapTopBar.MenuAction.Reload:
                OpenUnlessUnsaved(_document.Id);
                break;
            case MapTopBar.MenuAction.Duplicate when _selected is not null:
                OnCardMenu(_selected, MapMoveAction.Duplicate);
                break;
            case MapTopBar.MenuAction.Delete when _selected is not null:
                OnCardMenu(_selected, MapMoveAction.Delete);
                break;
            case MapTopBar.MenuAction.RaisePriority when _selected is not null:
                OnCardMenu(_selected, MapMoveAction.RaisePriority);
                break;
            case MapTopBar.MenuAction.LowerPriority when _selected is not null:
                OnCardMenu(_selected, MapMoveAction.LowerPriority);
                break;
        }
    }

    private void OnCardMenu(string moveId, MapMoveAction action)
    {
        int index = IndexOf(_document!.MoveIds, moveId);
        if (index < 0)
        {
            return;
        }

        switch (action)
        {
            case MapMoveAction.NewAfter:
                _fileActions.AskNew(_document, _context!, index);
                break;
            case MapMoveAction.Duplicate:
                _fileActions.AskDuplicate(_document, _context!, index);
                break;
            case MapMoveAction.Delete:
                _fileActions.AskDelete(_document, index);
                break;
            case MapMoveAction.RaisePriority or MapMoveAction.LowerPriority:
                int other = index + (action == MapMoveAction.RaisePriority ? -1 : 1);
                if (other >= 0 && other < _document.MoveIds.Count)
                {
                    _document.SwapMoves(index, other);
                    _status = "Input priority changed";
                    OnListChanged();
                }

                break;
        }
    }

    // The art file only ever holds timing and sparks the game reads (ArtTimingFile checks each edit), so it saves even
    // when the data files can't.
    private void Save()
    {
        _art?.Save();
        _problems = _saver!.Save(_document!);
        _status = _problems.Count == 0 ? $"Saved {_document!.Id}" : "Not saved: fix the problems first";
        ShowMap(refit: false);
    }

    private void ShowState() => _topBar.ShowState(
        _document?.MoveIds.Count, _problems.Count, HasChanges, _status);

    private int? SelectedIndex() =>
        _selected is not null && IndexOf(_document!.MoveIds, _selected) is int index and >= 0 ? index : null;

    // Each shown move's category, for arrows into a whole category.
    private Dictionary<string, string> Categories(MoveMapLayout layout)
    {
        Dictionary<string, string> categories = new(StringComparer.Ordinal);
        foreach (MapColumn column in layout.Columns)
        {
            foreach (string entry in column.Entries)
            {
                if (_document!.Move(entry) is EditedFile move && JsonNodeValue.Text(move.Root[MoveFields.Category]) is string category)
                {
                    categories[entry] = category;
                }
            }
        }

        return categories;
    }

    private static string? FirstMove(MoveMapLayout layout)
    {
        foreach (MapColumn column in layout.Columns)
        {
            foreach (string entry in column.Entries)
            {
                if (entry != MoveMapLayout.JumpEntry)
                {
                    return entry;
                }
            }
        }

        return null;
    }

    private static int IndexOf(IReadOnlyList<string> items, string item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item)
            {
                return i;
            }
        }

        return -1;
    }
}
#endif
