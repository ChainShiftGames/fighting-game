using Sim.Input.Motions;
using Sim.Rules;

namespace Sim.Data.Motions;

/// <summary>The loaded <c>input/motions.json</c>: the motion set and the input timing (developer settings, ADR-0002).</summary>
public sealed class MotionFile
{
    public MotionFile(MotionSet motions, int motionWindow, int buttonBuffer, MovementInputs movement)
    {
        Motions = motions;
        MotionWindow = motionWindow;
        ButtonBuffer = buttonBuffer;
        Movement = movement;
    }

    /// <summary>In priority order.</summary>
    public MotionSet Motions { get; }

    public int MotionWindow { get; }

    public int ButtonBuffer { get; }

    /// <summary>The motions (and optional button) that dash, backdash and high jump.</summary>
    public MovementInputs Movement { get; }

    /// <summary>Copies the input timing into the match rules, where the logic reads it.</summary>
    public void ApplyTo(ref MatchRules rules)
    {
        rules.MotionWindow = MotionWindow;
        rules.ButtonBuffer = ButtonBuffer;
    }
}
