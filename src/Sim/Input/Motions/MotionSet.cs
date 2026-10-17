using System;
using Sim.State;

namespace Sim.Input.Motions;

/// <summary>The motions of the game in priority order, built from data (developer setting, ADR-0002).</summary>
/// <remarks>
/// Index 0 has the highest priority: when several motions are input on the same frame, the first one wins
/// (<see cref="MotionParser.FindFirst"/>). Read-only during a match and not part of the game state; it is part of
/// the data hash.
/// </remarks>
public sealed class MotionSet
{
    public const int MaxMotions = MotionMask.Capacity;

    /// <summary>No motion: not found, or nothing was input.</summary>
    public const int NoMotion = -1;

    private readonly int[] _notations;
    private readonly int[] _windows;
    private readonly int[] _shortcuts;
    private readonly int[] _stepStarts;
    private readonly int[] _stepCounts;
    private readonly MotionStep[] _steps;

    /// <exception cref="ArgumentException">
    /// The set is empty or too large, a notation is repeated or not positive, a motion has no steps or an invalid
    /// one, two consecutive steps share a direction, a window is outside the input history, or a shortcut direction
    /// isn't 1–9 or is used by two motions.
    /// </exception>
    public MotionSet(ReadOnlySpan<MotionDefinition> motions)
    {
        if (motions.IsEmpty || motions.Length > MaxMotions)
        {
            throw new ArgumentException($"A motion set needs 1 to {MaxMotions} motions.", nameof(motions));
        }

        _notations = new int[motions.Length];
        _windows = new int[motions.Length];
        _shortcuts = new int[motions.Length];
        _stepStarts = new int[motions.Length];
        _stepCounts = new int[motions.Length];
        int stepTotal = 0;
        for (int motion = 0; motion < motions.Length; motion++)
        {
            Check(motions[motion], motions[..motion]);
            _notations[motion] = motions[motion].Notation;
            _windows[motion] = motions[motion].Window;
            _shortcuts[motion] = motions[motion].Shortcut;
            _stepStarts[motion] = stepTotal;
            _stepCounts[motion] = motions[motion].Steps.Length;
            stepTotal += motions[motion].Steps.Length;
        }

        _steps = new MotionStep[stepTotal];
        for (int motion = 0; motion < motions.Length; motion++)
        {
            motions[motion].Steps.CopyTo(_steps, _stepStarts[motion]);
        }
    }

    public int Count => _notations.Length;

    public int NotationOf(int motion) => _notations[motion];

    /// <summary>The motion's own window, or <see cref="MotionDefinition.UsesMatchWindow"/>.</summary>
    public int WindowOf(int motion) => _windows[motion];

    /// <summary>The motion's special button shortcut direction, or <see cref="MotionDefinition.NoShortcut"/>.</summary>
    public int ShortcutOf(int motion) => _shortcuts[motion];

    /// <summary>Oldest step first.</summary>
    public ReadOnlySpan<MotionStep> StepsOf(int motion) => _steps.AsSpan(_stepStarts[motion], _stepCounts[motion]);

    /// <summary>Finds a motion by its notation; <see cref="NoMotion"/> when the set doesn't define it.</summary>
    public bool TryFind(int notation, out int motion)
    {
        for (motion = 0; motion < _notations.Length; motion++)
        {
            if (_notations[motion] == notation)
            {
                return true;
            }
        }

        motion = NoMotion;
        return false;
    }

    private static void Check(MotionDefinition motion, ReadOnlySpan<MotionDefinition> earlier)
    {
        if (motion.Notation <= 0)
        {
            throw new ArgumentException($"Motion notation {motion.Notation} must be positive.", nameof(motion));
        }

        foreach (MotionDefinition other in earlier)
        {
            if (other.Notation == motion.Notation)
            {
                throw new ArgumentException($"Motion {motion.Notation} is defined twice.", nameof(motion));
            }

            // One press must name one motion, or the shortcut would be ambiguous.
            if (motion.Shortcut != MotionDefinition.NoShortcut && other.Shortcut == motion.Shortcut)
            {
                throw new ArgumentException(
                    $"Motions {other.Notation} and {motion.Notation} share shortcut direction {motion.Shortcut}.",
                    nameof(motion));
            }
        }

        if (motion.Shortcut is < MotionDefinition.NoShortcut or > 9)
        {
            throw new ArgumentException(
                $"Motion {motion.Notation} has shortcut {motion.Shortcut}; it must be a numpad direction 1 to 9.",
                nameof(motion));
        }

        if (motion.Window < MotionDefinition.UsesMatchWindow || motion.Window > InputHistory.Capacity)
        {
            throw new ArgumentException(
                $"Motion {motion.Notation} has window {motion.Window}; the input history holds " +
                $"{InputHistory.Capacity} frames.", nameof(motion));
        }

        CheckSteps(motion);
    }

    private static void CheckSteps(MotionDefinition motion)
    {
        MotionStep[]? steps = motion.Steps;
        if (steps is null || steps.Length == 0)
        {
            throw new ArgumentException($"Motion {motion.Notation} needs at least one step.", nameof(motion));
        }

        for (int step = 0; step < steps.Length; step++)
        {
            if (!steps[step].IsValid)
            {
                throw new ArgumentException(
                    $"Step {step} of motion {motion.Notation} must accept numpad directions 1 to 9 only.",
                    nameof(motion));
            }

            // Holding one direction would otherwise complete both steps.
            if (step > 0 && steps[step].Overlaps(steps[step - 1]))
            {
                throw new ArgumentException(
                    $"Steps {step - 1} and {step} of motion {motion.Notation} share a direction.", nameof(motion));
            }
        }
    }
}
