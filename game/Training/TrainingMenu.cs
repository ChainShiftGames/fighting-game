using System;
using Godot;

namespace FightingGame.Training;

/// <summary>
/// Placeholder training menu (keys only, until the real menus): a summary line at the bottom while training, and the
/// full list of <see cref="ITrainingMenuEntry"/> lines when opened. While open it takes every key press.
/// </summary>
/// <remarks>The game keeps running while it is open, so a changed dummy setting can be seen at once.</remarks>
public partial class TrainingMenu : CanvasLayer
{
    // Placeholder menu keys (developer settings until the menus, which will follow the player's bindings).
    private const Key OpenKey = Key.Tab;
    private const Key CloseKey = Key.Escape;

    // Above the match, below the options menu.
    private const int MenuLayer = 5;
    private const float SummaryHeight = 32f;
    private static readonly Color _backdrop = new(0f, 0f, 0f, 0.7f);

    private readonly ColorRect _background = new() { Color = _backdrop, AnchorRight = 1f, AnchorBottom = 1f };
    private readonly Label _menu = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AnchorRight = 1f,
        AnchorBottom = 1f,
    };

    private readonly Label _summary = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Bottom,
        AnchorTop = 1f,
        AnchorRight = 1f,
        AnchorBottom = 1f,
        OffsetTop = -SummaryHeight,
    };

    private ITrainingMenuEntry[] _entries = [];
    private bool _isOpen;

    public override void _Ready()
    {
        Layer = MenuLayer;
        AddChild(_background);
        AddChild(_menu);
        AddChild(_summary);
        Refresh();
    }

    /// <summary>Shows the menu for a training match that just started; empty entries hide it (no training running).</summary>
    public void Use(ITrainingMenuEntry[] entries)
    {
        _entries = entries;
        _isOpen = _isOpen && entries.Length > 0;
        Refresh();
    }

    public override void _Process(double delta) => Refresh();

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (_entries.Length == 0 || inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        Key pressed = key.PhysicalKeycode;
        if (!_isOpen)
        {
            if (pressed == OpenKey)
            {
                _isOpen = true;
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (pressed is OpenKey or CloseKey)
        {
            _isOpen = false;
        }
        else
        {
            Array.Find(_entries, entry => entry.Key == pressed)?.Press();
        }

        GetViewport().SetInputAsHandled();
    }

    private void Refresh()
    {
        bool isTraining = _entries.Length > 0;
        _background.Visible = isTraining && _isOpen;
        _menu.Visible = isTraining && _isOpen;
        _summary.Visible = isTraining && !_isOpen;
        if (_menu.Visible)
        {
            _menu.Text = TrainingMenuText.Describe(_entries, OpenKey);
        }
        else if (_summary.Visible)
        {
            _summary.Text = TrainingMenuText.Summary(_entries, OpenKey);
        }
    }
}
