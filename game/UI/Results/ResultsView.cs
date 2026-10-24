using System.Collections.Generic;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.Results;

/// <summary>
/// Draws <see cref="ResultsScreen"/> over the finished match with the shared menu theme: the winner in their colour,
/// rounds and session score, then the entries with each player's cursor and rematch tick beside them. Only reads the
/// screen.
/// </summary>
/// <remarks>The menus show it in place of <see cref="MenuView"/> while the results are on top.</remarks>
public partial class ResultsView : CanvasLayer
{
    // The same layer as MenuView: above the match and the training menu, below the options menu.
    private const int MenuLayer = 6;
    private const float EntryWidth = 420f;
    private const float MarkerSize = 36f;

    // Under the HUD's round banner, which already announces the winner in large letters: the title here is a
    // smaller line in the winner's colour, so the whole column fits below the banner.
    private const float OverMatchTop = 0.44f;
    private const int TitleFontSize = 34;
    private const int ColumnSeparation = 4;

    private static readonly Color _neutral = new(1f, 1f, 1f);

    private readonly Label _title = new() { ThemeTypeVariation = "MenuTitle", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _body = new() { ThemeTypeVariation = "MenuBody", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly VBoxContainer _entries = new() { ThemeTypeVariation = "MenuItems" };
    private readonly Label _hint = new() { ThemeTypeVariation = "MenuItemDescription", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly CenterContainer _centre = new() { AnchorRight = 1f, AnchorBottom = 1f, AnchorTop = OverMatchTop };
    private readonly List<Row> _rows = [];
    private MenuTextFormatter? _text;
    private MenuTransition? _transition;
    private ResultsScreen? _shown;

    public void Bind(MenuTextFormatter text, MenuTransition transition)
    {
        _text = text;
        _transition = transition;
    }

    public override void _Ready()
    {
        Layer = MenuLayer;
        Visible = false;
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        var root = new Control { AnchorRight = 1f, AnchorBottom = 1f, MouseFilter = Control.MouseFilterEnum.Ignore };
        if (ResourceLoader.Exists(MenuView.ThemePath))
        {
            root.Theme = GD.Load<Theme>(MenuView.ThemePath);
        }

        AddChild(root);
        root.AddChild(new Panel { AnchorRight = 1f, AnchorBottom = 1f, ThemeTypeVariation = "MenuOverlayBackdrop" });
        root.AddChild(_centre);
        var column = new VBoxContainer { ThemeTypeVariation = "MenuColumn" };
        column.AddThemeConstantOverride("separation", ColumnSeparation);
        _title.AddThemeFontSizeOverride("font_size", TitleFontSize);
        column.AddChild(_title);
        column.AddChild(_body);
        column.AddChild(_entries);
        column.AddChild(_hint);
        _centre.AddChild(column);
    }

    /// <summary>Shows the screen as it is now, or hides when it is null; call once per displayed frame.</summary>
    public void Show(ResultsScreen? screen, long nowMilliseconds)
    {
        if (screen is null || _text is null)
        {
            Visible = false;
            _shown = null;
            return;
        }

        if (!ReferenceEquals(screen, _shown))
        {
            _shown = screen;
            _transition!.Start(nowMilliseconds);
        }

        Visible = true;
        _centre.Modulate = new Color(1f, 1f, 1f, _transition!.Opacity(nowMilliseconds));
        int winner = screen.Result.WinnerSide;
        _title.Text = _text.Format(screen.Title);
        _title.AddThemeColorOverride("font_color", winner >= 0 ? SelectStyles.Of(winner) : _neutral);
        _body.Text = _text.FormatLines(screen.Body);
        _body.Visible = _body.Text.Length > 0;
        _hint.Text = _text.Format(screen.Hint);
        _hint.Visible = _hint.Text.Length > 0;
        ShowEntries(screen);
    }

    private void ShowEntries(ResultsScreen screen)
    {
        IReadOnlyList<ResultsEntry> entries = screen.Choices.Entries;
        while (_rows.Count < entries.Count)
        {
            _rows.Add(AddRow());
        }

        for (int index = 0; index < _rows.Count; index++)
        {
            Row row = _rows[index];
            row.Box.Visible = index < entries.Count;
            if (row.Box.Visible)
            {
                ShowRow(screen, row, index, entries[index]);
            }
        }
    }

    private void ShowRow(ResultsScreen screen, Row row, int index, ResultsEntry entry)
    {
        row.Label.Text = _text!.Format(screen.LabelOf(entry));
        bool isFocused = false;
        for (int side = 0; side < 2; side++)
        {
            ResultsMarkerShape shape = MarkerOn(screen, side, index, entry, out Color colour);
            row.Markers[side].Show(shape, colour, pointsLeft: side == 1);
            isFocused |= shape == ResultsMarkerShape.Cursor;
        }

        row.Panel.ThemeTypeVariation = isFocused ? "MenuItemFocused" : "MenuItem";
    }

    // A tick beside rematch for a player who wants it; otherwise the cursor of whoever points at the entry. A cursor
    // that is nobody's (replays) is drawn on P1's side in white.
    private static ResultsMarkerShape MarkerOn(
        ResultsScreen screen, int side, int index, ResultsEntry entry, out Color colour)
    {
        colour = SelectStyles.Of(side);
        if (entry == ResultsEntry.Rematch && screen.WantsRematch(side))
        {
            return ResultsMarkerShape.Tick;
        }

        ResultsChoices choices = screen.Choices;
        for (int chooser = 0; chooser < choices.Choosers; chooser++)
        {
            int drawnSide = screen.SideOf(chooser);
            bool isHere = (drawnSide < 0 ? 0 : drawnSide) == side && choices.CursorOf(chooser) == index;
            if (isHere && screen.CanChoose(chooser) && !choices.IsIn(chooser))
            {
                colour = drawnSide < 0 ? _neutral : colour;
                return ResultsMarkerShape.Cursor;
            }
        }

        return ResultsMarkerShape.None;
    }

    private Row AddRow()
    {
        var box = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        var markers = new ResultsMarker[2];
        markers[0] = NewMarker();
        var panel = new PanelContainer { ThemeTypeVariation = "MenuItem", CustomMinimumSize = new Vector2(EntryWidth, 0f) };
        var label = new Label { ThemeTypeVariation = "MenuItemLabel", HorizontalAlignment = HorizontalAlignment.Center };
        panel.AddChild(label);
        markers[1] = NewMarker();
        box.AddChild(markers[0]);
        box.AddChild(panel);
        box.AddChild(markers[1]);
        _entries.AddChild(box);
        return new Row(box, panel, label, markers);
    }

    private static ResultsMarker NewMarker() => new()
    {
        CustomMinimumSize = new Vector2(MarkerSize, MarkerSize),
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private sealed record Row(HBoxContainer Box, PanelContainer Panel, Label Label, ResultsMarker[] Markers);
}
