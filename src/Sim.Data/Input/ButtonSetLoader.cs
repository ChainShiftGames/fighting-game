using System;
using System.Collections.Generic;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;

namespace Sim.Data.Input;

/// <summary>Reads and validates <c>input/buttons.json</c> into the game logic's <see cref="ButtonSet"/> (ADR-0010).</summary>
public static class ButtonSetLoader
{
    /// <summary>The button set, or null when anything was reported to <paramref name="errors"/>.</summary>
    public static ButtonSet? Load(IDataSource source, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        FieldReader? file = JsonDataFile.Open(source, ButtonSetSchema.File, errors);
        if (file is null || !SchemaVersion.Check(file, ButtonSetSchema.Version))
        {
            return null;
        }

        file.Ignore("$schema");
        IReadOnlyList<FieldReader?> entries = file.ObjectArray("buttons", required: true);
        if (entries.Count == 0)
        {
            file.Error("buttons", "needs at least one button");
        }

        List<ButtonDefinition> buttons = ReadButtons(entries);
        file.Finish();

        // Every rule ButtonSet enforces was checked above, so it can't throw here.
        return errors.Count > errorsBefore ? null : new ButtonSet(buttons.ToArray());
    }

    private static List<ButtonDefinition> ReadButtons(IReadOnlyList<FieldReader?> entries)
    {
        List<ButtonDefinition> buttons = [];
        foreach (FieldReader? entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            string? id = entry.RequiredId("id");
            int bit = entry.RequiredInt("bit", InputWord.FirstButtonBit, InputWord.BitCount - 1);
            entry.Finish();
            if (id is null)
            {
                continue;
            }

            ReportRepeats(entry, buttons, id, bit);
            buttons.Add(new ButtonDefinition(id, bit));
        }

        return buttons;
    }

    private static void ReportRepeats(FieldReader entry, List<ButtonDefinition> earlier, string id, int bit)
    {
        foreach (ButtonDefinition button in earlier)
        {
            if (string.Equals(button.Id, id, StringComparison.Ordinal))
            {
                entry.Error("id", $"button '{id}' is defined twice");
            }

            if (button.Bit == bit)
            {
                entry.Error("bit", $"bit {bit} is already used by button '{button.Id}'");
            }
        }
    }
}
