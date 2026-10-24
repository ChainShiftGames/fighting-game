using System.Collections.Generic;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// Draws <see cref="CharacterSelectScreen"/> with the shared menu theme: the title, player 1's preview, the grid with
/// both cursors, player 2's preview, the controls line and the "both ready" banner (online: waiting for the other
/// PC's pick). Only reads the selection.
/// </summary>
/// <remarks>The menus show it in place of <see cref="MenuView"/> while character select is on top.</remarks>
public partial class CharacterSelectView : CanvasLayer
{
    // The same layer as MenuView: above the match and the training menu, below the options menu.
    private const int MenuLayer = 6;
    private const int GridSpacing = 10;
    private const int ColumnSpacing = 48;

    private readonly Label _title = new() { ThemeTypeVariation = "MenuTitle", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _problem = new()
    {
        ThemeTypeVariation = "MenuBody",
        HorizontalAlignment = HorizontalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private readonly GridContainer _grid = new();
    private readonly List<CharacterCard> _cards = [];
    private readonly SidePreview[] _sides = [new(), new()];
    private readonly Label _hint = new() { ThemeTypeVariation = "MenuItemDescription", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _allReady = new()
    {
        ThemeTypeVariation = "MenuTitle",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AnchorRight = 1f,
        AnchorBottom = 1f,
    };

    // A dimmed band across the middle, so the banner reads over the grid.
    private readonly Panel _allReadyBand = new()
    {
        ThemeTypeVariation = "MenuOverlayBackdrop",
        AnchorTop = 0.4f,
        AnchorRight = 1f,
        AnchorBottom = 0.6f,
    };

    private readonly Control _root = new()
    {
        AnchorRight = 1f,
        AnchorBottom = 1f,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private MenuTextFormatter? _text;
    private MenuTransition? _transition;
    private CharacterSelectScreen? _shown;

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
        Control root = _root;
        if (ResourceLoader.Exists(MenuView.ThemePath))
        {
            root.Theme = GD.Load<Theme>(MenuView.ThemePath);
        }

        AddChild(root);
        root.AddChild(new Panel { AnchorRight = 1f, AnchorBottom = 1f, ThemeTypeVariation = "MenuScreenBackdrop" });
        var centre = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        root.AddChild(centre);
        var column = new VBoxContainer { ThemeTypeVariation = "MenuColumn" };
        centre.AddChild(column);
        column.AddChild(_title);
        column.AddChild(_problem);

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", ColumnSpacing);
        _sides[0].For(0);
        _sides[1].For(1);
        _grid.AddThemeConstantOverride("h_separation", GridSpacing);
        _grid.AddThemeConstantOverride("v_separation", GridSpacing);
        _grid.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(_sides[0]);
        row.AddChild(_grid);
        row.AddChild(_sides[1]);
        column.AddChild(row);
        column.AddChild(_hint);
        _allReadyBand.AddChild(_allReady);
        root.AddChild(_allReadyBand);
    }

    /// <summary>Shows the screen as it is now, or hides when it is null; call once per displayed frame.</summary>
    public void Show(CharacterSelectScreen? screen, long nowMilliseconds)
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
        _root.Modulate = new Color(1f, 1f, 1f, _transition!.Opacity(nowMilliseconds));

        CharacterSelection selection = screen.Selection;
        _title.Text = _text.Format(screen.Title);
        _problem.Text = _text.FormatLines(screen.Body);
        _problem.Visible = _problem.Text.Length > 0;
        ShowGrid(selection);
        _sides[0].Show(screen, _text);
        _sides[1].Show(screen, _text);
        _hint.Text = _text.Format(MenuText.Key("MENU_SELECT_HINT"));
        string banner = screen.IsWaitingForOpponent ? "ONLINE_SELECT_WAITING" : "MENU_SELECT_ALL_READY";
        _allReady.Text = _text.Format(MenuText.Key(banner));
        _allReadyBand.Visible = selection.IsAllReady;
    }

    private void ShowGrid(CharacterSelection selection)
    {
        CharacterGrid grid = selection.Grid;
        bool hasCharacters = grid.CharacterCount > 0;
        _grid.Visible = hasCharacters;
        _grid.Columns = grid.Columns;
        while (_cards.Count < grid.SlotCount)
        {
            var card = new CharacterCard();
            _cards.Add(card);
            _grid.AddChild(card);
        }

        for (int slot = 0; slot < _cards.Count; slot++)
        {
            CharacterCard card = _cards[slot];
            card.Visible = slot < grid.SlotCount;
            if (!card.Visible)
            {
                continue;
            }

            string name = grid.IsRandom(slot)
                ? _text!.Format(MenuText.Key("MENU_SELECT_RANDOM"))
                : selection.Characters[slot].Name;
            card.Show(name, IsCursorOn(selection, 0, slot), IsCursorOn(selection, 1, slot));
        }
    }

    private static bool IsCursorOn(CharacterSelection selection, int side, int slot) =>
        selection.Chooses(side) && selection.Side(side).Slot == slot;
}
