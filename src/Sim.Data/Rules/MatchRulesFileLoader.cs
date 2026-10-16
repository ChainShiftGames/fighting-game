using Sim.Data.Sources;
using Sim.Data.Validation;

namespace Sim.Data.Rules;

/// <summary>Reads and validates <c>rules/match.json</c>.</summary>
public static class MatchRulesFileLoader
{
    /// <summary>The match rules values, or null when anything was reported to <paramref name="errors"/>.</summary>
    public static MatchRulesFile? Load(IDataSource source, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        FieldReader? file = JsonDataFile.Open(source, MatchRulesSchema.File, errors);
        if (file is null || !SchemaVersion.Check(file, MatchRulesSchema.Version))
        {
            return null;
        }

        file.Ignore("$schema");
        int roundStartDistance = file.RequiredInt("roundStartDistance", 0, MatchRulesSchema.MaxDistance);
        file.Finish();
        return errors.Count > errorsBefore ? null : new MatchRulesFile(roundStartDistance);
    }
}
