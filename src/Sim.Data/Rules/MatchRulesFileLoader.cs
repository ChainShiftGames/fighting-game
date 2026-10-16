using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Rules;
using Sim.State;

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
        TradeRule tradeRule = file.RequiredChoice("tradeRule", MatchRulesSchema.TradeRules);
        if (errors.Count == errorsBefore)
        {
            CheckFitsTheStage(file, roundStartDistance, wallDistance, maxPlayerDistance);
        }

        RoundFormat rounds = ReadRounds(file, errors);

        // The match state has a fixed number of slots per player; the setting picks how many are used.
        int entitiesPerPlayer = file.RequiredInt("entitiesPerPlayer", 1, EntityPool.MaxPerPlayer);
        file.Finish();
        return errors.Count > errorsBefore
            ? null
            : new MatchRulesFile(
                roundStartDistance, wallDistance, maxPlayerDistance, tradeRule, rounds, entitiesPerPlayer);
    }

    private static RoundFormat ReadRounds(FieldReader file, DataErrors errors)
    {
        int errorsBefore = errors.Count;
        int timerFrames = file.RequiredInt("roundTimerFrames", 1, MatchRulesSchema.MaxRoundTimerFrames);
        int roundsToWin = file.RequiredInt("roundsToWin", 1, MatchRulesSchema.MaxRoundsToWin);
        int maxRounds = file.RequiredInt("maxRounds", 1, MatchRulesSchema.MaxRounds);
        int roundOverFrames = file.RequiredInt("roundOverFrames", 1, MatchRulesSchema.MaxRoundOverFrames);
        int fewestRoundsToDecide = (2 * roundsToWin) - 1;
        // Only once every value read cleanly: a missing or out-of-range one is the real mistake to report.
        if (errors.Count == errorsBefore && maxRounds < fewestRoundsToDecide)
        {
            file.Error(
                "maxRounds",
                $"must be at least 2 * roundsToWin - 1 ({fewestRoundsToDecide}), or a match without draws can run " +
                "out of rounds before anyone wins");
        }

        return new RoundFormat(timerFrames, roundsToWin, maxRounds, roundOverFrames);
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
