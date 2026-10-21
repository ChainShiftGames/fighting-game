#if TOOLS
using System;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Presentation.World;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// Edits how a move's animation lines up with its frames (ADR-0028): the <see cref="AnimationTrack"/> (shown by the
/// Timing tab under its frame timeline, so the two scroll together), a stepper for the clip frame the picked frame
/// shows, and a button to remove its key. Edits go into the character's art file and retime the preview's model at
/// once; the art file is written on Save.
/// </summary>
[Tool]
public partial class AnimationTimingPanel : VBoxContainer
{
    private readonly Label _note = new()
    {
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        ThemeTypeVariation = MapTheme.MutedLabel,
    };

    private readonly HBoxContainer _controls = new();
    private readonly NumberStepper _clip = new();
    private readonly Button _unpin = new() { Text = "Remove key", ThemeTypeVariation = MapTheme.Ghost };
    private ModelPreview? _preview;
    private DrawerMove? _move;
    private AnimationTrackData? _data;
    private MoveAnimationTiming _timing = MoveAnimationTiming.None;
    private int _frame = 1;

    public AnimationTimingPanel()
    {
        AddThemeConstantOverride("separation", 6);
        _unpin.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _controls.AddChild(_clip);
        _controls.AddChild(_unpin);
        AddChild(_controls);
        AddChild(_note);
        _clip.ValueChanged += clip => Pin(_frame, clip);
        _unpin.Pressed += () => Edit(_timing.Unpin(_data!.Phases, _frame));
        Track.Pinned += Pin;
        Track.Unpinned += frame => Edit(_timing.Unpin(_data!.Phases, frame));
    }

    /// <summary>The timing changed: the preview shows the new pose and the art file has unsaved changes.</summary>
    public event Action? Changed;

    /// <summary>The track, for the Timing tab to place under its frame timeline.</summary>
    public AnimationTrack Track { get; } = new();

    /// <summary>The model the preview draws, whose clips this retimes and reads.</summary>
    public void UsePreview(ModelPreview preview) => _preview = preview;

    /// <summary>Shows the move's timing as its art file has it now, unsaved edits included.</summary>
    public void ShowMove(DrawerMove move)
    {
        _move = move;
        _timing = move.Art is { Problem.Length: 0 } art ? art.Timing(move.Id) : MoveAnimationTiming.None;
        Retime();
        ShowTrack(string.Empty);
    }

    /// <summary>The frame data changed: keys stretch with their phases.</summary>
    public void ShowFrames() => ShowTrack(string.Empty);

    /// <summary>The picked frame: the stepper edits what it shows.</summary>
    public void MarkFrame(int frame)
    {
        _frame = frame;
        Track.MarkFrame(frame);
        ShowControls();
    }

    private void Pin(int frame, int clip)
    {
        if (_data is not null && _timing.Pin(_data.Phases, frame, clip, _data.Part) is { } pinned)
        {
            Edit(pinned);
        }
    }

    private void Edit(MoveAnimationTiming timing)
    {
        DrawerMove move = _move!;
        string problem = move.Art!.SetTiming(move.Id, timing);
        if (problem.Length == 0)
        {
            _timing = timing;
            problem = Retime();
            Changed?.Invoke();
        }

        ShowTrack(problem);
    }

    private string Retime() => _move!.Art is { Problem.Length: 0 } && _preview is not null
        ? _preview.Retime(_move.Id, _timing.Contacts, _timing.Keys)
        : string.Empty;

    private void ShowTrack(string problem)
    {
        DrawerMove move = _move!;
        AnimationClipTiming? clip = _preview?.TimingOf(move.Id);
        string? cantEdit = move.Art switch
        {
            null => "No art file: the character is drawn as a placeholder, so there is no animation to line up.",
            { Problem.Length: > 0 } art => $"The art file can't be read, so the timing can't be edited: {art.Problem}",
            _ when clip is null => "No clip for this move: name one in the art file.",
            _ when move.IsBasic => "A basic move plays its clip frame for frame or looping: nothing to line up.",
            _ when clip is { Fit: true } => "This clip is fitted over the whole move (fit): nothing to line up.",
            _ => null,
        };
        _data = cantEdit is null ? AnimationTrackData.Of(move.Root, clip!.Value, _timing) : null;
        Track.ShowTrack(_data);
        Track.Visible = _data is not null;
        _note.Text = cantEdit ?? (problem.Length > 0 ? problem
            : "Drag a pin up or down to change the pose on its frame. Double-click a frame to pin it; right-click a key to remove it.");
        ShowControls();
    }

    private void ShowControls()
    {
        _controls.Visible = _data is not null && _frame >= 1 && _frame <= _data.ClipFrames.Count;
        if (!_controls.Visible)
        {
            return;
        }

        AnimationTrackData data = _data!;
        TrackPin? pin = data.PinOn(_frame);
        string what = pin?.Kind switch
        {
            TrackPinKind.ActiveFrom => "the contact pose",
            TrackPinKind.RecoveryFrom => "recovery starts",
            TrackPinKind.Key => "timing key",
            _ => _frame == 1 ? "the clip's start" : "not pinned · changing it pins it",
        };
        _clip.Minimum = data.Part.Start;
        _clip.Maximum = data.Part.End;
        _clip.Show($"Frame {_frame} shows clip frame", what, pin?.Clip ?? data.ClipAt(_frame));
        _clip.Visible = _frame > 1;
        _unpin.Visible = pin is { Kind: TrackPinKind.Key };
    }
}
#endif
