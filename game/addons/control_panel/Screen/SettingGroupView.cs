#if TOOLS
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.ControlPanel.Catalog;
using FightingGame.Addons.ControlPanel.Documents;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// One <see cref="SettingGroup"/> as a card like the Character tab's: its title (with the group's on/off switch, e.g.
/// a mechanic's <c>enabled</c>), a hint line and a row per setting.
/// </summary>
/// <remarks>
/// A card with a switch glows in the accent colour while on, like the Character tab's selected move, and sinks into
/// the background while off, so a page of mechanics shows at a glance which are in the game.
/// </remarks>
[Tool]
public partial class SettingGroupView : PanelContainer
{
    private const int Columns = 4;

    private readonly List<SettingRow> _rows = [];
    private PanelDocument? _document;
    private PanelSetting? _switch;
    private CheckButton? _switchButton;
    private Label? _title;
    private PanelLook? _look;
    private StyleBox? _onStyle;
    private StyleBox? _offStyle;

    /// <summary>Whether the card has an on/off switch (a mechanic).</summary>
    public bool HasSwitch => _switch is not null;

    /// <summary>True when the card has a switch and it is on.</summary>
    public bool IsOn { get; private set; }

    /// <summary>A stand-in for the card's height before it is laid out: its rows plus the header.</summary>
    public int EstimatedRows => _rows.Count + 3;

    public void Build(SettingGroup group, PanelDocument document, PanelLook look, SettingTooltips tooltips)
    {
        _document = document;
        _switch = group.Switch;
        _look = look;
        ThemeTypeVariation = MapTheme.Card;
        if (group.Switch is not null && look.Theme.GetStylebox("panel", MapTheme.Card) is StyleBoxFlat card)
        {
            var glowing = (StyleBoxFlat)card.Duplicate();
            glowing.BorderColor = MapPalette.Glow with { A = 0.55f };
            glowing.ShadowColor = MapPalette.Glow with { A = 0.08f };
            glowing.ShadowSize = look.Px(10f);
            var sunk = (StyleBoxFlat)card.Duplicate();
            sunk.BgColor = MapPalette.Panel;
            _onStyle = glowing;
            _offStyle = sunk;
        }

        var column = new VBoxContainer();
        column.AddChild(Header(group, tooltips));
        if (group.Hint.Length > 0)
        {
            column.AddChild(new Label
            {
                Text = group.Hint,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ThemeTypeVariation = MapTheme.MutedLabel,
            });
            column.AddChild(new HSeparator());
        }

        var grid = new GridContainer { Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", look.Px(12f));
        grid.AddThemeConstantOverride("v_separation", look.Px(6f));
        foreach (PanelSetting setting in group.Settings)
        {
            _rows.Add(new SettingRow(setting, document, grid, look, tooltips.For(setting)));
        }

        column.AddChild(grid);
        AddChild(column);
    }

    public void Refresh()
    {
        bool on = true;
        if (_switch is not null && _switchButton is not null && _document is not null)
        {
            try
            {
                on = _document.Flag(_switch);
                _switchButton.SetPressedNoSignal(on);
                _switchButton.Text = on ? "On" : "Off";
                _title!.AddThemeColorOverride(
                    "font_color", _document.IsChanged(_switch) ? _look!.Changed : MapPalette.Text);
                _switchButton.AddThemeColorOverride("font_color", on ? MapPalette.Good : MapPalette.Muted);
                _switchButton.AddThemeColorOverride("font_pressed_color", on ? MapPalette.Good : MapPalette.Muted);
                if ((on ? _onStyle : _offStyle) is StyleBox style)
                {
                    AddThemeStyleboxOverride("panel", style);
                }
            }
            catch (System.Exception exception) when (exception is System.FormatException or System.IO.IOException)
            {
                _switchButton.Disabled = true;
            }
        }

        IsOn = _switch is not null && on;
        foreach (SettingRow row in _rows)
        {
            row.Refresh();
            row.Dim(!on);
        }
    }

    private HBoxContainer Header(SettingGroup group, SettingTooltips tooltips)
    {
        var header = new HBoxContainer();
        _title = new Label
        {
            Text = group.Title,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = group.Hint,
            MouseFilter = MouseFilterEnum.Stop,
            ThemeTypeVariation = MapTheme.Title,
        };
        header.AddChild(_title);
        if (group.Switch is { } setting)
        {
            _switchButton = new CheckButton { TooltipText = tooltips.For(setting) };
            _switchButton.Toggled += pressed => _document!.SetFlag(setting, pressed);
            header.AddChild(_switchButton);
        }

        return header;
    }
}
#endif
