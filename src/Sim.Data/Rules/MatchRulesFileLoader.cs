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
        int wallDistance = file.RequiredInt("wallDistance", 1, MatchRulesSchema.MaxDistance);
        int maxPlayerDistance = file.RequiredInt("maxPlayerDistance", 1, 2 * MatchRulesSchema.MaxDistance);
        if (errors.Count == errorsBefore)
        {
            CheckFitsTheStage(file, roundStartDistance, wallDistance, maxPlayerDistance);
        }

        file.Finish();
        return errors.Count > errorsBefore
            ? null
            : new MatchRulesFile(roundStartDistance, wallDistance, maxPlayerDistance);
    }

    private static void CheckFitsTheStage(FieldReader file, int roundStartDistance, int wallDistance, int maxPlayerDistance)
    {
        if (roundStartDistance >= wallDistance)
        {
            file.Error("roundStartDistance", $"must be less than wallDistance ({wallDistance}), or rounds start in the wall");

            // No max distance fits such a stage, so reporting one too would only hide the real mistake.
            return;
        }

        if (maxPlayerDistance > 2 * wallDistance)
        {
            file.Error(
                "maxPlayerDistance",
                $"must be at most twice wallDistance ({2 * wallDistance}), or the camera sees past the stage walls");
        }
        else if (maxPlayerDistance <= 2 * roundStartDistance)
        {
            file.Error(
                "maxPlayerDistance",
                $"must be more than twice roundStartDistance ({2 * roundStartDistance}), or rounds start off screen");
        }
    }
}
