using System;
using System.Collections.Generic;
using System.Text;
using FightingGame.Presentation.Hud;
using Godot;

namespace FightingGame.UI;

/// <summary>
/// Placeholder menu look: a title, a few lines of text and a column of buttons in the middle of the screen, over a
/// backdrop. Keyboard (arrows, Enter, Escape back), joypad (d-pad, A confirms, B back) and mouse all move Godot's
/// focus; the buttons are rebuilt only when their labels change, so focus stays put while the text updates every
/// frame.
/// </summary>
public partial class MenuPanel : CanvasLayer
{
    // Above the match and the training menu, below the options menu.
    private const int MenuLayer = 6;
    private const int TitleSize = 52;
    private const int TextSize = 22;
    private const int ButtonSize = 26;
    private const float ButtonWidth = 420f;
    private const int Spacing = 14;

    // Presses this soon after the menu opens are ignored: they belong to the fight that just ended (button mashing).
    private const ulong InputGraceMilliseconds = 600;

    // Placeholder menu buttons (developer settings until the menus, which will follow the player's bindings).
    private const JoyButton ConfirmButton = JoyButton.A;
    private const JoyButton BackButton = JoyButton.B;
    private const Key BackKey = Key.Escape;

    private static readonly Color _screenBackdrop = new(0.05f, 0.06f, 0.09f);
    private static readonly Color _overlayBackdrop = new(0f, 0f, 0f, 0.45f);

    private readonly ColorRect _backdrop = new() { AnchorRight = 1f, AnchorBottom = 1f };
    private readonly CenterContainer _centre = new() { AnchorRight = 1f, AnchorBottom = 1f };
    private readonly Label _title = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _text = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly VBoxContainer _buttons = new();
    private IReadOnlyList<MenuEntry> _entries = [];
    private Action? _back;
    private string _shownEntries = string.Empty;
    private ulong _openedAt;

    public override void _Ready()
    {
        Layer = MenuLayer;
        Visible = false;
        AddChild(_backdrop);

        var root = new Control { AnchorRight = 1f, AnchorBottom = 1f, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.Theme = MenuTheme();
        AddChild(root);
        root.AddChild(_centre);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", Spacing);
        _title.AddThemeFontSizeOverride("font_size", TitleSize);
        _text.AddThemeFontSizeOverride("font_size", TextSize);
        _buttons.AddThemeConstantOverride("separation", Spacing / 2);
        column.AddChild(_title);
        column.AddChild(_text);
        column.AddChild(_buttons);
        _centre.AddChild(column);
    }

    /// <summary>Shows the menu, or updates it; call each displayed frame while it is up.</summary>
    /// <param name="overMatch">Over a match, which stays visible behind it (and the round banner above it).</param>
    /// <param name="back">What B or Escape does: back to the previous menu; null where there is none.</param>
    public void Show(string title, string text, IReadOnlyList<MenuEntry> entries, bool overMatch, Action? back = null)
    {
        if (!Visible)
        {
            Visible = true;
            _openedAt = Time.GetTicksMsec();
            _shownEntries = string.Empty;
        }

        _backdrop.Color = overMatch ? _overlayBackdrop : _screenBackdrop;

        // Over a match the column sits in the lower half, under the KO / winner banner.
        _centre.AnchorTop = overMatch ? 0.45f : 0f;
        _title.Text = title;
        _title.Visible = title.Length > 0;
        _text.Text = text;
        _text.Visible = text.Length > 0;
        _entries = entries;
        _back = back;
        string shown = title + '\n' + Describe(entries);
        if (shown != _shownEntries)
        {
            // Another menu starts at its first button; the same menu keeps the focused one.
            bool sameMenu = _shownEntries.StartsWith(title + '\n', StringComparison.Ordinal);
            _shownEntries = shown;
            Rebuild(sameMenu);
        }
    }

    /// <summary>Hides the menu; its buttons let go of focus so game input can't press them.</summary>
    public void Close()
    {
        if (!Visible)
        {
            return;
        }

        Visible = false;
        GetViewport().GuiReleaseFocus();
    }

    // A presses the focused button here, as Godot's ui_accept didn't for joypads; Enter and clicks go through Godot.
    // Unhandled only: an event the focused button already took never gets here, so nothing is pressed twice.
    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!Visible)
        {
            return;
        }

        bool confirm = inputEvent is InputEventJoypadButton { Pressed: true, ButtonIndex: ConfirmButton };
        bool back = inputEvent is InputEventJoypadButton { Pressed: true, ButtonIndex: BackButton } or
            InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: BackKey };
        if (confirm && GetViewport().GuiGetFocusOwner() is Button focused && focused.GetParent() == _buttons)
        {
            GetViewport().SetInputAsHandled();
            Choose(focused.GetIndex());
        }
        else if (back && _back is not null)
        {
            GetViewport().SetInputAsHandled();
            if (Time.GetTicksMsec() - _openedAt >= InputGraceMilliseconds)
            {
                _back();
            }
        }
    }

    /// <summary>Whether A or B is down on any joypad.</summary>
    public static bool IsMenuButtonHeld()
    {
        foreach (int device in Godot.Input.GetConnectedJoypads())
        {
            if (Godot.Input.IsJoyButtonPressed(device, ConfirmButton) || Godot.Input.IsJoyButtonPressed(device, BackButton))
            {
                return true;
            }
        }

        return false;
    }

    private static Theme MenuTheme()
    {
        var theme = new Theme { DefaultFontSize = ButtonSize };
        if (ResourceLoader.Exists(HudLettering.FontPath))
        {
            theme.DefaultFont = GD.Load<Font>(HudLettering.FontPath);
        }

        return theme;
    }

    private static string Describe(IReadOnlyList<MenuEntry> entries)
    {
        var text = new StringBuilder();
        foreach (MenuEntry entry in entries)
        {
            text.Append(entry.Enabled ? '+' : '-').Append(entry.Label).Append('\n');
        }

        return text.ToString();
    }

    // Keeps the focused position, so a label changing under the cursor (e.g. "Ready" → "Ready — waiting") doesn't move it.
    private void Rebuild(bool keepFocus)
    {
        int focused = 0;
        Control? owner = GetViewport().GuiGetFocusOwner();
        foreach (Node child in _buttons.GetChildren())
        {
            if (keepFocus && child == owner)
            {
                focused = child.GetIndex();
            }

            _buttons.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            int index = i;
            var button = new Button
            {
                Text = _entries[i].Label,
                Disabled = !_entries[i].Enabled,
                CustomMinimumSize = new Vector2(ButtonWidth, 0f),
            };
            button.Pressed += () => Choose(index);
            _buttons.AddChild(button);
        }

        if (_buttons.GetChildCount() > 0)
        {
            int index = Mathf.Clamp(focused, 0, _buttons.GetChildCount() - 1);
            _buttons.GetChild<Button>(index).CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    private void Choose(int index)
    {
        if (Time.GetTicksMsec() - _openedAt < InputGraceMilliseconds || index >= _entries.Count || !_entries[index].Enabled)
        {
            return;
        }

        _entries[index].Choose();
    }
}
