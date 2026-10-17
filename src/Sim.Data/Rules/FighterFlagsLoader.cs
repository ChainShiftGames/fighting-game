using System.Collections.Generic;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Fighters;
using Sim.Rules;

namespace Sim.Data.Rules;

/// <summary>Reads and validates <c>rules/flags.json</c>: the game-wide fighter flags, in bit order (ADR-0023).</summary>
public static class FighterFlagsLoader
{
    private const string FlagsField = "flags";

    /// <summary>The flags, or null when anything was reported to <paramref name="errors"/>.</summary>
    public static FighterFlagsFile? Load(IDataSource source, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        FieldReader? file = JsonDataFile.Open(source, FighterFlagsSchema.File, errors);
        if (file is null || !SchemaVersion.Check(file, FighterFlagsSchema.Version))
        {
            return null;
        }

        file.Ignore("$schema");
        IReadOnlyList<FieldReader?> entries = file.ObjectArray(FlagsField, required: true);
        file.Finish();
        if (entries.Count > FighterFlagNames.MaxFlags)
        {
            file.Error(FlagsField, $"lists {entries.Count} flags; at most {FighterFlagNames.MaxFlags} fit");
            return null;
        }

        List<string> names = [];
        int clearedWhenHit = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] is not { } entry)
            {
                continue;
            }

            string? name = entry.RequiredId("name");
            bool isCleared = entry.RequiredBool("clearedWhenHit");
            entry.Finish();
            if (name is not null && names.Contains(name))
            {
                entry.Error("name", $"'{name}' is listed twice");
            }

            names.Add(name ?? "");
            clearedWhenHit |= isCleared ? 1 << i : 0;
        }

        return errors.Count > errorsBefore ? null : new FighterFlagsFile(new FighterFlagNames(names), clearedWhenHit);
    }
}
