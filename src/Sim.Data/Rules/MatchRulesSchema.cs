namespace Sim.Data.Rules;

/// <summary>Version and path of the match rules file (data/schema/match.schema.json).</summary>
public static class MatchRulesSchema
{
    public const int Version = 1;

    public const string File = "rules/match.json";

    /// <summary>Upper bound for distances, the same as in character data.</summary>
    public const int MaxDistance = 1_000_000;
}
