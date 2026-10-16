using System.Collections.Generic;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input.Motions;
using Sim.State;

namespace Sim.Data.Motions;

/// <summary>Reads and validates <c>input/motions.json</c> into the game logic's <see cref="MotionSet"/> (ADR-0002).</summary>
public static class MotionSetLoader
{
    /// <summary>The motions and input timing, or null when anything was reported to <paramref name="errors"/>.</summary>
    /// <param name="source">The data root.</param>
    /// <param name="buttons">The loaded button set, for the optional dash button.</param>
    /// <param name="errors">Where problems are reported.</param>
    public static MotionFile? Load(IDataSource source, IButtonLookup buttons, DataErrors errors)
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
        MovementInputs movement = ReadMovementInputs(file.RequiredObject("movementInputs"), motions, buttons);
        file.Finish();

        // Every rule MotionSet enforces was checked above, so it can't throw here.
        return errors.Count > errorsBefore
            ? null
            : new MotionFile(new MotionSet(motions.ToArray()), motionWindow, buttonBuffer, movement);
    }

    private static MovementInputs ReadMovementInputs(
        FieldReader? entry, List<MotionDefinition> motions, IButtonLookup buttons)
    {
        if (entry is null)
        {
            return default;
        }

        int dash = ReadDefinedMotion(entry, "dash", motions);
        int backdash = ReadDefinedMotion(entry, "backdash", motions);
        int highJump = ReadDefinedMotion(entry, "highJump", motions);
        int dashButton = 0;
        string? buttonId = entry.OptionalId("dashButton");
        if (buttonId is not null && !buttons.TryGetMask(buttonId, out dashButton))
        {
            entry.Error("dashButton", $"unknown button \"{buttonId}\"; the button set has " +
                string.Join(", ", buttons.ButtonIds));
        }

        entry.Finish();
        return new MovementInputs(dash, backdash, highJump, dashButton);
    }

    private static int ReadDefinedMotion(FieldReader entry, string field, List<MotionDefinition> motions)
    {
        int notation = MotionTextReader.ReadNotation(entry, field);
        if (notation == 0)
        {
            return 0;
        }

        foreach (MotionDefinition motion in motions)
        {
            if (motion.Notation == notation)
            {
                return notation;
            }
        }

        entry.Error(field, $"motion {notation} isn't in the motions list");
        return 0;
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
