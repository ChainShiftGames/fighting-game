using Sim.Data.Hashing;

namespace Sim.Data.Rules;

/// <summary>Adds the match rules file to the data hash: every value in it changes how a match plays.</summary>
public static class MatchRulesDataHash
{
    public static void Append(DataHashBuilder hash, MatchRulesFile file)
    {
        hash.Add(file.RoundStartDistance);
        hash.Add(file.WallDistance);
        hash.Add(file.MaxPlayerDistance);
    }
}
