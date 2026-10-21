#if TOOLS
using System;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// Play and speed buttons over the drawer's preview: plays the move at match speed (or slower), looping, by showing
/// each frame in turn, so the model, the boxes and the timeline marker all follow.
/// </summary>
[Tool]
public partial class PreviewPlayback : HBoxContainer
{
    // Developer settings (editor): the slow-motion steps, and how long the last frame stays before the loop restarts.
    private static readonly double[] _speeds = [1d, 0.5d, 0.25d];
    private static readonly string[] _speedNames = ["1×", "½×", "¼×"];
    private const int EndHoldFrames = 20;

    private readonly Button _play = new() { ThemeTypeVariation = MapTheme.Chip, FocusMode = FocusModeEnum.None };
    private readonly Button _speed = new()
    {
        ThemeTypeVariation = MapTheme.Chip,
        FocusMode = FocusModeEnum.None,
        TooltipText = "Playback speed: match speed, half or quarter",
    };

    private readonly PlaybackClock _clock = new(EndHoldFrames);
    private int _speedIndex;

    public PreviewPlayback()
    {
        AddThemeConstantOverride("separation", 4);
        AddChild(_play);
        AddChild(_speed);
        _play.Pressed += () =>
        {
            if (IsPlaying)
            {
                Stop();
            }
            else
            {
                Play();
            }
        };
        _speed.Pressed += () =>
        {
            _speedIndex = (_speedIndex + 1) % _speeds.Length;
            ShowButtons();
        };
        SetProcess(false);
        ShowButtons();
    }

    /// <summary>The next frame is due; the owner shows it.</summary>
    public event Action<int>? FrameDue;

    public bool IsPlaying => IsProcessing();

    /// <summary>The move's length now, read each frame since frame data may change while it plays.</summary>
    public Func<int> TotalFrames { get; set; } = () => 1;

    /// <summary>The frame shown when Play is pressed, where playing starts.</summary>
    public Func<int> CurrentFrame { get; set; } = () => 1;

    public void Stop()
    {
        SetProcess(false);
        ShowButtons();
    }

    public override void _Process(double delta)
    {
        _clock.TotalFrames = TotalFrames();
        _clock.Speed = _speeds[_speedIndex];
        int before = _clock.Frame;
        int frame = _clock.Advance(delta);
        if (frame != before)
        {
            FrameDue?.Invoke(frame);
        }
    }

    private void Play()
    {
        _clock.TotalFrames = TotalFrames();
        _clock.StartAt(CurrentFrame());
        FrameDue?.Invoke(_clock.Frame);
        SetProcess(true);
        ShowButtons();
    }

    private void ShowButtons()
    {
        _play.Text = IsPlaying ? "❚❚ Pause" : "▶ Play";
        _play.TooltipText = IsPlaying ? "Stop on this frame" : "Play the move, looping";
        _speed.Text = _speedNames[_speedIndex];
    }
}
#endif
