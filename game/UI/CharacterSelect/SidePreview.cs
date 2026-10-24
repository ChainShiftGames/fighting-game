using System.Collections.Generic;
using System.Globalization;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// One player's side of character select: their tag, a large placeholder portrait in the colour shown, the name, the
/// one-line description, the colour swatches and how far they are.
/// </summary>
public partial class SidePreview : VBoxContainer
{
    private const float PortraitWidth = 300f;
    private const float PortraitHeight = 220f;
    private const float SwatchSize = 26f;

    private static readonly Color _portrait = new(1f, 1f, 1f, 0.1f);
    private static readonly Color _none = new(0f, 0f, 0f, 0f);
    private static readonly Color _focusedSwatch = new(1f, 1f, 1f, 1f);
    private static readonly Color _takenSwatch = new(0f, 0f, 0f, 0.65f);

    private readonly Label _tag = new() { ThemeTypeVariation = "MenuTitle", HorizontalAlignment = HorizontalAlignment.Center };
    private readonly PanelContainer _portraitPanel = new();
    private readonly Label _portraitName = new()
    {
        ThemeTypeVariation = "MenuTitle",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private readonly Label _description = new()
    {
        ThemeTypeVariation = "MenuItemDescription",
        HorizontalAlignment = HorizontalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private readonly HBoxContainer _swatches = new() { Alignment = AlignmentMode.Center };
    private readonly List<StyleBoxFlat> _swatchStyles = [];
    private readonly Label _status = new()
    {
        ThemeTypeVariation = "MenuValue",
        HorizontalAlignment = HorizontalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };
    private StyleBoxFlat? _portraitStyle;
    private int _side;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(PortraitWidth, 0f);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        AddThemeConstantOverride("separation", 10);
        _portraitStyle = SelectStyles.Box(_portrait, SelectStyles.Of(_side), 3, 12f);
        _portraitPanel.AddThemeStyleboxOverride("panel", _portraitStyle);
        _portraitPanel.CustomMinimumSize = new Vector2(PortraitWidth, PortraitHeight);
        _portraitPanel.AddChild(_portraitName);
        _tag.AddThemeColorOverride("font_color", SelectStyles.Of(_side));
        AddChild(_tag);
        AddChild(_portraitPanel);
        AddChild(_description);
        AddChild(_swatches);
        AddChild(_status);
    }

    /// <summary>Which player this side is; call before it is added to the tree.</summary>
    public void For(int side) => _side = side;

    public void Show(CharacterSelectScreen screen, MenuTextFormatter text)
    {
        CharacterSelection selection = screen.Selection;
        _tag.Text = text.Format(MenuText.Key("MENU_SELECT_PLAYER", (_side + 1).ToString(CultureInfo.InvariantCulture)));
        (int character, int colour) = selection.Shown(_side);
        SelectableCharacter? shown = character >= 0 ? selection.Characters[character] : null;
        _portraitName.Text = shown is null ? "?" : shown.Name;
        _portraitStyle!.BgColor = shown is null ? _portrait : Tint(shown.Colours[colour]);
        _description.Text = shown is null
            ? text.Format(IsRandomShown(selection) ? MenuText.Key("MENU_SELECT_RANDOM_DESCRIPTION") : MenuText.Empty)
            : text.Format(shown.Description);
        ShowSwatches(selection, shown, character, colour);
        _status.Text = text.Format(Status(screen, shown, colour));
    }

    private bool IsRandomShown(CharacterSelection selection) =>
        selection.Chooses(_side) && selection.Grid.IsRandom(selection.Side(_side).Slot);

    private MenuText Status(CharacterSelectScreen screen, SelectableCharacter? shown, int colour)
    {
        CharacterSelection selection = screen.Selection;
        if (selection.IsRemote(_side))
        {
            return MenuText.Key(selection.HasRemotePick ? "MENU_SELECT_READY" : "ONLINE_SELECT_OPPONENT_PICKING");
        }

        if (!selection.Chooses(_side))
        {
            return MenuText.Key(screen.IsTraining ? "MENU_SELECT_DUMMY" : "MENU_SELECT_NO_CONTROLLER");
        }

        return selection.Side(_side).Step switch
        {
            SelectStep.Colour => MenuText.Key(
                "MENU_SELECT_COLOUR",
                (colour + 1).ToString(CultureInfo.InvariantCulture),
                shown!.Colours.Count.ToString(CultureInfo.InvariantCulture)),
            SelectStep.Ready => MenuText.Key("MENU_SELECT_READY"),
            _ => MenuText.Key("MENU_SELECT_CHOOSE"),
        };
    }

    // The colours of the character shown, the current one framed and the other player's locked-in one dimmed.
    private void ShowSwatches(CharacterSelection selection, SelectableCharacter? shown, int character, int colour)
    {
        int count = shown?.Colours.Count ?? 0;
        while (_swatchStyles.Count < count)
        {
            var style = SelectStyles.Box(_none, _none, 3, 0f);
            var swatch = new Panel { CustomMinimumSize = new Vector2(SwatchSize, SwatchSize) };
            swatch.AddThemeStyleboxOverride("panel", style);
            _swatchStyles.Add(style);
            _swatches.AddChild(swatch);
        }

        for (int i = 0; i < _swatchStyles.Count; i++)
        {
            var swatch = (Control)_swatches.GetChild(i);
            swatch.Visible = i < count;
            if (i >= count)
            {
                continue;
            }

            bool isTaken = selection.IsTaken(_side, character, i);
            Color tint = Color.FromHtml(shown!.Colours[i]);
            _swatchStyles[i].BgColor = isTaken ? tint.Lerp(_takenSwatch, 0.7f) : tint;
            _swatchStyles[i].BorderColor = i == colour ? _focusedSwatch : _none;
        }
    }

    // The portrait is a placeholder: its colour stands in for the palette.
    private static Color Tint(string colour)
    {
        Color tint = Color.FromHtml(colour);
        return new Color(tint.R, tint.G, tint.B, 0.35f);
    }
}
