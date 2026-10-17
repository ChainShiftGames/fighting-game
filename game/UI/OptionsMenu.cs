using System;
using FightingGame.Settings;
using Godot;

namespace FightingGame.UI;

/// <summary>
/// Placeholder options menu (keys only, until the real menus): the player settings, opened over whatever is running.
/// While open it takes every key press, so the lobby menu underneath doesn't act on them.
/// </summary>
public partial class OptionsMenu : CanvasLayer
{
    // Placeholder menu keys (developer settings until the menus, which will follow the player's bindings).
    private const Key OpenKey = Key.F12;
    private const Key CloseKey = Key.Escape;
    private const Key VsyncKey = Key.Key1;
    private const Key InputDelayKey = Key.Key2;

    // Above the match status and lobby menu, which it covers while open.
    private const int MenuLayer = 10;
    private static readonly Color _backdrop = new(0f, 0f, 0f, 0.85f);

    private readonly Label _label = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AnchorRight = 1f,
        AnchorBottom = 1f,
    };

    private Func<PlayerSettings>? _current;
    private Action<PlayerSettings>? _change;

    public override void _Ready()
    {
        Layer = MenuLayer;
        Visible = false;
        AddChild(new ColorRect { Color = _backdrop, AnchorRight = 1f, AnchorBottom = 1f });
        AddChild(_label);
    }

    /// <param name="current">The settings as they are now (they may also change outside the menu).</param>
    /// <param name="change">Applies and saves a changed copy.</param>
    public void Bind(Func<PlayerSettings> current, Action<PlayerSettings> change)
    {
        _current = current;
        _change = change;
    }

    public override void _Process(double delta)
    {
        if (Visible && _current is not null)
        {
            _label.Text = OptionsMenuText.Describe(_current());
        }
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (_current is null || _change is null || inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        Key pressed = key.PhysicalKeycode;
        if (!Visible)
        {
            if (pressed == OpenKey)
            {
                Visible = true;
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        PlayerSettings settings = _current();
        if (pressed is OpenKey or CloseKey)
        {
            Visible = false;
        }
        else if (pressed == VsyncKey)
        {
            _change(settings with { Vsync = !settings.Vsync });
        }
        else if (pressed == InputDelayKey)
        {
            _change(settings with { InputDelayOverrideFrames = InputDelayOption.Next(settings.InputDelayOverrideFrames) });
        }

        GetViewport().SetInputAsHandled();
    }
}
