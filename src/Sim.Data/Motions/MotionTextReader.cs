using System;
using Sim.Data.Characters.Reading;
using Sim.Data.Validation;
using Sim.Input.Motions;

namespace Sim.Data.Motions;

/// <summary>Reads a motion's notation ("236") and steps ("2 3 6", "123" for any down) from their JSON text.</summary>
public static class MotionTextReader
{
    /// <summary>The notation as a number, as moves store it; 0 when it was reported.</summary>
    public static int ReadNotation(FieldReader entry, string field)
    {
        string text = entry.RequiredText(field);
        int notation = 0;
        foreach (char digit in text)
        {
            if (digit is < '1' or > '9' || text.Length > CharacterSchema.MaxMotionLength)
            {
                entry.Error(field, $"must be numpad notation, 1 to {CharacterSchema.MaxMotionLength} digits from " +
                    $"1 to 9 (e.g. \"236\"), got \"{text}\"");
                return 0;
            }

            notation = (notation * 10) + (digit - '0');
        }

        return notation;
    }

    /// <summary>The steps oldest first; null when anything was reported.</summary>
    public static MotionStep[]? ReadSteps(FieldReader entry, string field)
    {
        string text = entry.RequiredText(field);
        if (text.Length == 0)
        {
            return null;
        }

        string[] groups = text.Split(' ');
        MotionStep[] steps = new MotionStep[groups.Length];
        for (int step = 0; step < groups.Length; step++)
        {
            int directions = ReadDirections(groups[step]);
            if (directions == 0)
            {
                entry.Error(field, "must be steps of numpad digits 1 to 9 separated by single spaces, no digit twice " +
                    $"in a step (e.g. \"2 3 6\" or \"123\"), got \"{text}\"");
                return null;
            }

            steps[step] = new MotionStep(directions);
            if (step > 0 && steps[step].Overlaps(steps[step - 1]))
            {
                entry.Error(field, $"steps {step} and {step + 1} share a direction, so holding it would complete both " +
                    $"(put a different direction between them, e.g. \"2 5 2\"), got \"{text}\"");
                return null;
            }
        }

        return steps;
    }

    // The step's direction bits, or 0 when the group is empty or has a wrong or repeated digit.
    private static int ReadDirections(ReadOnlySpan<char> group)
    {
        int directions = 0;
        foreach (char digit in group)
        {
            int bit = 1 << (digit - '0');
            if (digit is < '1' or > '9' || (directions & bit) != 0)
            {
                return 0;
            }

            directions |= bit;
        }

        return directions;
    }
}
