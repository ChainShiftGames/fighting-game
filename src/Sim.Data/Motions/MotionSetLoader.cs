using System.Collections.Generic;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input.Motions;
using Sim.State;

namespace Sim.Data.Motions;

/// <summary>Reads and validates <c>input/motions.json</c> into the game logic's <see cref="MotionSet"/> (ADR-0002).</summary>
public static class MotionSetLoader
{
    /// <summary>The motions and input timing, or null when anything was reported to <paramref name="errors"/>.</summary>
    public static MotionFile? Load(IDataSource source, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        FieldReader? file = JsonDataFile.Open(source, MotionSetSchema.File, errors);
        if (file is null || !SchemaVersion.Check(file, MotionSetSchema.Version))
        {
            return null;
        }

        file.Ignore("$schema");
        int motionWindow = file.RequiredInt("motionWindow", 1, InputHistory.Capacity);
        int buttonBuffer = file.RequiredInt("buttonBuffer", 0, InputHistory.Capacity - 1);
        CheckFitsHistory(file, "motionWindow", motionWindow, buttonBuffer);
        IReadOnlyList<FieldReader?> entries = file.ObjectArray("motions", required: true);
        if (entries.Count == 0 || entries.Count > MotionSet.MaxMotions)
        {
            file.Error("motions", $"needs 1 to {MotionSet.MaxMotions} motions, got {entries.Count}");
        }

        List<MotionDefinition> motions = ReadMotions(entries, buttonBuffer);
        file.Finish();

        // Every rule MotionSet enforces was checked above, so it can't throw here.
        return errors.Count > errorsBefore
            ? null
            : new MotionFile(new MotionSet(motions.ToArray()), motionWindow, buttonBuffer);
    }

    private static List<MotionDefinition> ReadMotions(IReadOnlyList<FieldReader?> entries, int buttonBuffer)
    {
        List<MotionDefinition> motions = [];
        foreach (FieldReader? entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            int notation = MotionTextReader.ReadNotation(entry, "motion");
            MotionStep[]? steps = MotionTextReader.ReadSteps(entry, "steps");
            int window = entry.OptionalInt("window", MotionDefinition.UsesMatchWindow, 1, InputHistory.Capacity);
            CheckFitsHistory(entry, "window", window, buttonBuffer);
            entry.Finish();
            if (notation == 0 || steps is null)
            {
                continue;
            }

            foreach (MotionDefinition earlier in motions)
            {
                if (earlier.Notation == notation)
                {
                    entry.Error("motion", $"motion {notation} is defined twice");
                }
            }

            motions.Add(new MotionDefinition(notation, steps, window));
        }

        return motions;
    }

    // The parser looks back a window plus the buffer, and the input history only holds so many frames.
    private static void CheckFitsHistory(FieldReader reader, string field, int window, int buttonBuffer)
    {
        if (window + buttonBuffer > InputHistory.Capacity)
        {
            reader.Error(field, $"{window} plus buttonBuffer {buttonBuffer} is longer than the " +
                $"{InputHistory.Capacity}-frame input history");
        }
    }
}
