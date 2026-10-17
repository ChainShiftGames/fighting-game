using System;
using Sim.Rules;
using Sim.State;

namespace Sim.Input.Motions;

/// <summary>Detects motions (236, 623…) and buffered button presses in a fighter's input history.</summary>
/// <remarks>
/// Stateless: it reads only the history, so rollback needs nothing extra. The rules (also in <c>data/README.md</c>):
/// <list type="bullet">
/// <item>The trigger is the button press or, for a motion without a button, the frame its last direction was
/// entered. It counts while it is at most <see cref="MatchRules.ButtonBuffer"/> frames old.</item>
/// <item>Every step lies within the motion's window, counted back from the trigger frame (inclusive). Steps come in
/// order but other directions may come between them, so 6236 also completes 623. The motion must be complete by
/// the press; directions after it don't count.</item>
/// <item>Directions are read with the fighter's current facing.</item>
/// </list>
/// Built once at match start; checks never allocate.
/// </remarks>
public sealed class MotionParser
{
    private readonly int[] _windows;
    private readonly int _buttonBuffer;

    /// <exception cref="ArgumentException">
    /// The window is under 1 frame, the buffer is negative, or a window plus the buffer is longer than the input
    /// history.
    /// </exception>
    public MotionParser(MotionSet motions, in MatchRules rules)
    {
        if (rules.MotionWindow < 1 || rules.ButtonBuffer < 0)
        {
            throw new ArgumentException(
                $"Motion window {rules.MotionWindow} must be at least 1 and button buffer {rules.ButtonBuffer} at " +
                "least 0.", nameof(rules));
        }

        Motions = motions;
        _buttonBuffer = rules.ButtonBuffer;
        _windows = new int[motions.Count];
        for (int motion = 0; motion < motions.Count; motion++)
        {
            int window = motions.WindowOf(motion);
            _windows[motion] = window == MotionDefinition.UsesMatchWindow ? rules.MotionWindow : window;
            if (_windows[motion] + _buttonBuffer > InputHistory.Capacity)
            {
                throw new ArgumentException(
                    $"Motion {motions.NotationOf(motion)}: window {_windows[motion]} plus button buffer " +
                    $"{_buttonBuffer} is longer than the {InputHistory.Capacity}-frame input history.", nameof(rules));
            }
        }
    }

    public MotionSet Motions { get; }

    /// <summary>Frames a button press stays valid (<see cref="MatchRules.ButtonBuffer"/>).</summary>
    public int ButtonBuffer => _buttonBuffer;

    /// <summary>The motion was input, with a press of every button in <paramref name="buttonMask"/>.</summary>
    /// <param name="motion">Index in <see cref="Motions"/>.</param>
    /// <param name="history">The fighter's cleaned input history.</param>
    /// <param name="facing">+1 facing right, -1 facing left.</param>
    /// <param name="buttonMask">Input word bits; 0 for a motion without a button (e.g. a 66 dash).</param>
    public bool IsInput(int motion, in InputHistory history, int facing, int buttonMask)
    {
        for (int trigger = 0; trigger <= _buttonBuffer; trigger++)
        {
            if (IsInputAt(motion, in history, facing, buttonMask, trigger))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The highest-priority motion of <paramref name="candidates"/> that was input, or <see cref="MotionSet.NoMotion"/>.
    /// </summary>
    public int FindFirst(MotionMask candidates, in InputHistory history, int facing, int buttonMask)
    {
        // Perf: almost no frame has a fresh press, and without one no motion with a button can match.
        if (buttonMask != 0 && !ButtonPress.IsPressedWithin(in history, buttonMask, _buttonBuffer))
        {
            return MotionSet.NoMotion;
        }

        for (int motion = 0; motion < Motions.Count; motion++)
        {
            if (candidates.Contains(motion) && IsInput(motion, in history, facing, buttonMask))
            {
                return motion;
            }
        }

        return MotionSet.NoMotion;
    }

    private bool IsInputAt(int motion, in InputHistory history, int facing, int buttonMask, int trigger)
    {
        ReadOnlySpan<MotionStep> steps = Motions.StepsOf(motion);
        int last = steps.Length - 1;
        int oldestFrame = trigger + _windows[motion] - 1;
        if (buttonMask != 0)
        {
            return ButtonPress.IsPressedAt(in history, buttonMask, trigger) &&
                StepsMatch(steps, last, in history, facing, trigger, oldestFrame);
        }

        bool isEntered = steps[last].Accepts(DirectionAt(in history, trigger, facing)) &&
            !steps[last].Accepts(DirectionAt(in history, trigger + 1, facing));
        return isEntered && StepsMatch(steps, last - 1, in history, facing, trigger + 1, oldestFrame);
    }

    // Walks back from the newest frame, matching steps from the last one to the first. Taking the newest frame that
    // fits a step never rules out a match for the earlier steps, so this greedy walk finds every match.
    private static bool StepsMatch(
        ReadOnlySpan<MotionStep> steps, int step, in InputHistory history, int facing, int newestFrame, int oldestFrame)
    {
        for (int frame = newestFrame; step >= 0 && frame <= oldestFrame; frame++)
        {
            if (steps[step].Accepts(DirectionAt(in history, frame, facing)))
            {
                step--;
            }
        }

        return step < 0;
    }

    private static int DirectionAt(in InputHistory history, int framesAgo, int facing) =>
        NumpadDirection.Of(history.FramesAgo(framesAgo), facing);
}
