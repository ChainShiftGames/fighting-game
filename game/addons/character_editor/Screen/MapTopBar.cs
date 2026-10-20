#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The editor's top bar: which character is open, which moves the map shows (<see cref="MapLens"/>), whether the
/// data checks out (click for the problems), a menu of file actions, and Save.
/// </summary>
[Tool]
public partial class MapTopBar : PanelContainer
{
    /// <summary>What the "…" menu offers.</summary>
    public enum MenuAction
    {
        NewMove,
        Duplicate,
        Delete,
        RaisePriority,
        LowerPriority,
        CharacterFile,
        Reload,
    }

    private static readonly (MapLens Lens, string Name)[] _lenses =
        [(MapLens.Combos, "Combos"), (MapLens.SpecialMoves, "Special moves"), (MapLens.Everything, "Everything")];

    private readonly OptionButton _characters = new() { ThemeTypeVariation = MapTheme.Flat, Flat = true, TooltipText = "Folders in data/characters/" };
    private readonly Label _subtitle = new() { Text = "· move map", ThemeTypeVariation = MapTheme.Title };
    private readonly ButtonGroup _lensGroup = new();
    private readonly Label _status = new() { ThemeTypeVariation = MapTheme.MutedLabel };
    private readonly Button _chip = new() { ThemeTypeVariation = MapTheme.Chip, FocusMode = FocusModeEnum.None };
    private readonly MenuButton _menu = new() { Text = "•••", TooltipText = "More: new move, duplicate, delete, input priority, character file, reload" };
    private readonly GradientButton _save = new() { Text = "Save", TooltipText = "Check the data, then write the changed files" };
    private readonly HBoxContainer _row = new();

    public MapTopBar()
    {
        _row.AddThemeConstantOverride("separation", 16);
        foreach (Control control in new Control[]
        {
            Title(), Lenses(), new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill }, _status, _chip, _menu, _save,
        })
        {
            _row.AddChild(control);
        }

        AddChild(_row);
        _characters.ItemSelected += index => CharacterChosen?.Invoke(_characters.GetItemMetadata((int)index).AsString());
        _chip.Pressed += () => ProblemsPressed?.Invoke(_chip);
        _save.Pressed += () => SavePressed?.Invoke();
        PopupMenu menu = _menu.GetPopup();
        menu.AddItem("New move…", (int)MenuAction.NewMove);
        menu.AddItem("Duplicate selected move…", (int)MenuAction.Duplicate);
        menu.AddItem("Delete selected move…", (int)MenuAction.Delete);
        menu.AddSeparator();
        menu.AddItem("Check selected input earlier (raise priority)", (int)MenuAction.RaisePriority);
        menu.AddItem("Check selected input later (lower priority)", (int)MenuAction.LowerPriority);
        menu.AddSeparator();
        menu.AddItem("Character stats and states…", (int)MenuAction.CharacterFile);
        menu.AddItem("Reload from disk", (int)MenuAction.Reload);
        menu.IdPressed += id => MenuPicked?.Invoke((MenuAction)(int)id);
    }

    public event Action<string>? CharacterChosen;

    public event Action<MapLens>? LensChosen;

    /// <summary>The status chip was clicked; the chip, to place the problem list under it.</summary>
    public event Action<Control>? ProblemsPressed;

    public event Action<MenuAction>? MenuPicked;

    public event Action? SavePressed;

    public override void _Ready()
    {
        MapLook look = MapLook.Of(this);
        var panel = new StyleBoxFlat { BgColor = MapPalette.Panel, BorderColor = MapPalette.Line, BorderWidthBottom = 1 };
        panel.ContentMarginLeft = panel.ContentMarginRight = look.Px(18f);
        panel.ContentMarginTop = panel.ContentMarginBottom = look.Px(10f);
        AddThemeStyleboxOverride("panel", panel);
        _characters.AddThemeFontOverride("font", look.Display);
        _characters.AddThemeFontSizeOverride("font_size", look.FontSize(18f));
        _row.CustomMinimumSize = new Vector2(0f, look.Px(34f));
    }

    /// <summary>Lists the characters (shown by their folder name in words) and selects one, without an event.</summary>
    public void ShowCharacters(IReadOnlyList<string> ids, string? selected)
    {
        _characters.Clear();
        foreach (string id in ids)
        {
            _characters.AddItem(MoveNames.Words(id));
            _characters.SetItemMetadata(_characters.ItemCount - 1, id);
        }

        Select(selected);
    }

    /// <summary>Shows the character as open, e.g. after a switch was cancelled.</summary>
    public void Select(string? id)
    {
        for (int i = 0; i < _characters.ItemCount; i++)
        {
            if (_characters.GetItemMetadata(i).AsString() == id)
            {
                _characters.Select(i);
                return;
            }
        }

        _characters.Select(-1);
    }

    /// <param name="moveCount">Moves in the open character; null when none is open.</param>
    public void ShowState(int? moveCount, int problemCount, bool hasChanges, string status)
    {
        _save.Disabled = !hasChanges;
        _save.Marked = hasChanges;
        _status.Text = hasChanges ? $"{status} · not saved yet" : status;
        _chip.Visible = moveCount is not null;
        _chip.Text = problemCount == 0
            ? string.Create(CultureInfo.InvariantCulture, $"✓ {moveCount} moves, no problems")
            : string.Create(CultureInfo.InvariantCulture, $"⚠ {problemCount} problem{(problemCount == 1 ? "" : "s")} · show");
        Color color = problemCount == 0 ? MapPalette.Good : MapPalette.Warn;
        foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color" })
        {
            _chip.AddThemeColorOverride(name, color);
        }
    }

    /// <summary>Without a data folder there is nothing to open or save.</summary>
    public void Disable(string reason)
    {
        _characters.Disabled = true;
        _menu.Disabled = true;
        _save.Disabled = true;
        _chip.Visible = false;
        _status.Text = reason;
    }

    private Control Title()
    {
        var title = new HBoxContainer();
        title.AddThemeConstantOverride("separation", 2);
        title.AddChild(_characters);
        _subtitle.AddThemeColorOverride("font_color", MapPalette.Muted);
        title.AddChild(_subtitle);
        return title;
    }

    private Control Lenses()
    {
        var frame = new PanelContainer();
        var box = new StyleBoxFlat { BgColor = MapPalette.Background, BorderColor = MapPalette.Line };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(10);
        box.SetContentMarginAll(3);
        frame.AddThemeStyleboxOverride("panel", box);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 2);
        foreach ((MapLens lens, string name) in _lenses)
        {
            var button = new Button
            {
                Text = name,
                ThemeTypeVariation = MapTheme.Segment,
                ToggleMode = true,
                ButtonGroup = _lensGroup,
                ButtonPressed = lens == MapLens.Combos,
                FocusMode = FocusModeEnum.None,
            };
            button.Pressed += () => LensChosen?.Invoke(lens);
            row.AddChild(button);
        }

        frame.AddChild(row);
        return frame;
    }
}
#endif
