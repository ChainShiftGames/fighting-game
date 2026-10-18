using System;
using Godot;

namespace FightingGame.VfxLab.Panels;

/// <summary>Playback: play/pause, frame steps, a scrub bar over the loop, speed and which stretch loops.</summary>
public partial class LabTransportBar : HFlowContainer
{
    private readonly Button _play = new() { CustomMinimumSize = new Vector2(70f, 0f) };
    private readonly HSlider _scrub = new() { MinValue = 0, Step = 1, CustomMinimumSize = new Vector2(320f, 0f), SizeFlagsVertical = SizeFlags.ShrinkCenter };
    private readonly Label _time = new() { CustomMinimumSize = new Vector2(190f, 0f) };
    private LabClock _clock = null!;
    private int _shown;

    /// <summary>Call once, before the bar joins the scene.</summary>
    /// <param name="loopChanged">The looped stretch changed (the options already hold it).</param>
    public void Build(LabClock clock, LabViewOptions options, Action loopChanged)
    {
        _clock = clock;
        _play.Pressed += () => clock.Playing = !clock.Playing;
        AddChild(_play);
        AddChild(StepButton("⏮", "Back to the loop's first frame (Home)", () => clock.Step(-_shown)));
        AddChild(StepButton("−10", "10 frames back (Shift+←)", () => clock.Step(-10)));
        AddChild(StepButton("−1", "One frame back (←)", () => clock.Step(-1)));
        AddChild(StepButton("+1", "One frame on (→)", () => clock.Step(1)));
        AddChild(StepButton("+10", "10 frames on (Shift+→)", () => clock.Step(10)));
        _scrub.ValueChanged += value => clock.Step((int)value - _shown);
        _scrub.DragStarted += () => clock.Playing = false;
        AddChild(_scrub);
        AddChild(_time);

        OptionButton speed = LabChoice.Of("Speed", ["¼×", "½×", "1×", "2×"], 2, index => clock.Speed = new[] { 0.25, 0.5, 1.0, 2.0 }[index]);
        AddChild(speed);
        AddChild(LabChoice.Of("Loop", ["Whole cast", "Charge only", "Flight only", "Impact only"], (int)options.Loop, index =>
        {
            options.Loop = (LabPhaseLoop)index;
            loopChanged();
        }));
    }

    /// <summary>Shows where playback is, every displayed frame.</summary>
    public void ShowTime(LabMoment moment, int loopLength, int phaseLength)
    {
        _play.Text = _clock.Playing ? "Pause" : "Play";
        _scrub.MaxValue = Math.Max(1, loopLength - 1);
        _shown = moment.LoopFrame;
        _scrub.SetValueNoSignal(moment.LoopFrame);
        _time.Text = moment.Phase == LabPhase.Rest
            ? $"f {moment.LoopFrame,3}  ·  rest"
            : $"f {moment.LoopFrame,3}  ·  {moment.Phase} {moment.PhaseFrame}/{phaseLength}";
    }

    private static Button StepButton(string text, string tooltip, Action pressed)
    {
        Button button = new() { Text = text, TooltipText = tooltip };
        button.Pressed += pressed;
        return button;
    }
}
