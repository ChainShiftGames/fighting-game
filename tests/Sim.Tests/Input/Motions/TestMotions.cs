using System;
using Sim.Input.Motions;
using Sim.Rules;
using Xunit;

namespace Sim.Tests.Input.Motions;

/// <summary>Motion sets and rules built in code for tests, so they don't depend on loading data.</summary>
internal static class TestMotions
{
    public const int Window = 12;
    public const int Buffer = 2;

    /// <summary>Same motions, steps and order as <c>data/input/motions.json</c>.</summary>
    public static MotionSet Default() => new(
    [
        Motion(632146, "6 3 2 1 4 6"),
        Motion(236236, "2 3 6 2 3 6"),
        Motion(63214, "6 3 2 1 4"),
        Motion(41236, "4 1 2 3 6"),
        Motion(623, "6 2 3", shortcut: 6),
        Motion(214, "2 1 4", shortcut: 4),
        Motion(236, "2 3 6", shortcut: 5),
        Motion(22, "2 5 2"),
        Motion(66, "6 5 6"),
        Motion(44, "4 5 4"),
        Motion(1, "1", window: 1),
        Motion(3, "3", window: 1),
        Motion(4, "4", window: 1),
        Motion(6, "6", window: 1),
        Motion(7, "7", window: 1),
        Motion(8, "8", window: 1),
        Motion(9, "9", window: 1),
        Motion(2, "123", window: 1),
        Motion(5, "123456789", window: 1),
    ]);

    public static MatchRules Rules(int window = Window, int buffer = Buffer)
    {
        MatchRules rules = default;
        rules.MotionWindow = window;
        rules.ButtonBuffer = buffer;
        return rules;
    }

    public static MotionParser Parser() => new(Default(), Rules());

    /// <param name="steps">Steps separated by spaces, each listing its directions, as in the data file.</param>
    public static MotionDefinition Motion(
        int notation,
        string steps,
        int window = MotionDefinition.UsesMatchWindow,
        int shortcut = MotionDefinition.NoShortcut)
    {
        string[] groups = steps.Split(' ');
        MotionStep[] parsed = new MotionStep[groups.Length];
        for (int step = 0; step < groups.Length; step++)
        {
            int[] directions = Array.ConvertAll(groups[step].ToCharArray(), digit => digit - '0');
            parsed[step] = MotionStep.Of(directions);
        }

        return new MotionDefinition(notation, parsed, window, shortcut);
    }

    public static int IndexOf(MotionSet motions, int notation)
    {
        Assert.True(motions.TryFind(notation, out int motion), $"Motion {notation} is not in the set.");
        return motion;
    }
}
