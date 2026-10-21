#if TOOLS
using System;
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// How long the move takes: wind-up, hitting and cool-down frames (one length for a basic move), whether hit areas
/// and chains move with their phase, the frame-by-frame timeline, and the move's projectiles.
/// </summary>
[Tool]
public partial class TimingPage : VBoxContainer, IDrawerPage
{
    private readonly SectionTitle _title = SectionTitle.Of("How long it takes");
    private readonly PhaseBar _bar = new();
    private readonly HBoxContainer _steppers = new();
    private readonly NumberStepper _startup = new() { Minimum = 1, Maximum = CharacterSchema.MaxMoveFrames };
    private readonly NumberStepper _active = new() { Minimum = 1, Maximum = CharacterSchema.MaxMoveFrames };
    private readonly NumberStepper _recovery = new() { Minimum = 0, Maximum = CharacterSchema.MaxMoveFrames };
    private readonly NumberStepper _duration = new() { Minimum = 1, Maximum = CharacterSchema.MaxMoveFrames };
    private readonly Label _unreadable = new()
    {
        Text = "The frame data can't be read: fix it under Overview › All settings (see the problems).",
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        ThemeTypeVariation = MapTheme.MutedLabel,
    };

    private readonly CheckBox _moveRanges = new()
    {
        Text = "Move hit areas and chains with their phase",
        ButtonPressed = true,
        TooltipText = "Off: only the frame data changes; boxes, chain windows and projectile spawns keep their frames",
    };

    private readonly FrameTimeline _timeline = new();
    private readonly AnimationTimingPanel _animation = new();
    private readonly SectionTitle _projectilesTitle = SectionTitle.Of("Projectiles");
    private readonly VBoxContainer _projectiles = new();
    private DrawerMove? _move;

    public TimingPage()
    {
        AddThemeConstantOverride("separation", 10);
        foreach (NumberStepper stepper in new[] { _startup, _active, _recovery, _duration })
        {
            _steppers.AddChild(stepper);
            stepper.ValueChanged += _ => Apply();
        }

        var timeline = new ScrollContainer
        {
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        var rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 6);
        rows.AddChild(_timeline);
        rows.AddChild(_animation.Track);
        timeline.AddChild(rows);
        _timeline.FrameClicked += frame => FrameClicked?.Invoke(frame);
        _animation.Track.FrameClicked += frame => FrameClicked?.Invoke(frame);
        _animation.Changed += () => AnimationChanged?.Invoke();
        foreach (Control control in new Control[]
        {
            _title, _bar, _steppers, _unreadable, _moveRanges, SectionTitle.Of("Frame by frame", "click a frame to see it"),
            timeline, _animation, _projectilesTitle, _projectiles,
        })
        {
            AddChild(control);
        }
    }

    public event Action<bool>? Changed;

    /// <summary>A frame was clicked on the timeline or the animation track.</summary>
    public event Action<int>? FrameClicked;

    /// <summary>The animation timing changed: the art file, not the move's data file.</summary>
    public event Action? AnimationChanged;

    /// <summary>The model the preview draws, whose clips the animation track retimes.</summary>
    public void UsePreview(ModelPreview preview) => _animation.UsePreview(preview);

    public void ShowMove(DrawerMove move)
    {
        _move = move;
        FramePhases? phases = FramePhases.Of(move.Root);
        _unreadable.Visible = phases is null;
        _steppers.Visible = phases is not null;
        _startup.Visible = _active.Visible = _recovery.Visible = phases is { IsBasic: false };
        _duration.Visible = phases is { IsBasic: true };
        if (phases is { IsBasic: true })
        {
            _duration.Show("Lasts", "frames", phases.Duration, MapPalette.Loop);
        }
        else if (phases is not null)
        {
            _startup.Show("Wind-up", "startup · until it hits", phases.Startup, MapPalette.Windup);
            _active.Show("Hitting", "active · can hit", phases.Active, MapPalette.Active);
            _recovery.Show("Cool-down", "recovery · can't act", phases.Recovery, MapPalette.Recovery);
        }

        _animation.ShowMove(move);
        ShowDerived();
        ShowProjectiles(move);
    }

    /// <summary>Outlines the frame shown in the preview.</summary>
    public void MarkFrame(int frame)
    {
        _timeline.MarkFrame(frame);
        _animation.MarkFrame(frame);
    }

    private void Apply()
    {
        DrawerMove move = _move!;
        FramePhases phases = _duration.Visible
            ? FramePhases.Basic(_duration.Value)
            : FramePhases.Attack(_startup.Value, _active.Value, _recovery.Value);
        MoveFrameData.Set(move.Root, phases, _moveRanges.ButtonPressed, move.Context.MoveSchema.FieldNames);
        ShowDerived();
        // Projectile spawns may have moved with their phase.
        ShowProjectiles(move);
        Changed?.Invoke(true);
    }

    private void ShowDerived()
    {
        FramePhases? phases = FramePhases.Of(_move!.Root);
        _bar.ShowPhases(phases?.Lengths ?? []);
        _title.Show("How long it takes", phases is null ? "" : string.Create(CultureInfo.InvariantCulture,
            $"{phases.TotalFrames} frames · {DrawerMove.Seconds(phases.TotalFrames)}"));
        _timeline.ShowTimeline(MoveTimeline.Of(_move.Root));
        _animation.ShowFrames();
    }

    private void ShowProjectiles(DrawerMove move)
    {
        foreach (Node child in _projectiles.GetChildren())
        {
            _projectiles.RemoveChild(child);
            child.QueueFree();
        }

        int number = 0;
        foreach (System.Text.Json.Nodes.JsonObject projectile in ProjectileFields.All(move.Root))
        {
            var card = new ProjectileCard();
            _projectiles.AddChild(card);
            card.ShowProjectile(++number, projectile, FramePhases.Of(move.Root)?.TotalFrames ?? 1);
            card.Changed += () =>
            {
                ShowDerived();
                Changed?.Invoke(false);
            };
        }

        _projectilesTitle.Visible = number > 0;
    }
}
#endif
