using Sim.Data.Validation;
using Sim.Rules;

namespace Sim.Data.Rules;

/// <summary>Version and path of the match rules file (data/schema/match.schema.json).</summary>
public static class MatchRulesSchema
{
    public const int Version = 1;

    public const string File = "rules/match.json";

    /// <summary>Upper bound for distances, the same as in character data.</summary>
    public const int MaxDistance = 1_000_000;

    /// <summary>Upper bound for the round timer: an hour at 60 frames per second, far beyond any real round.</summary>
    public const int MaxRoundTimerFrames = 216_000;

    /// <summary>Upper bound for round wins and the round limit; sanity bounds, not design values.</summary>
    public const int MaxRoundsToWin = 10;

    public const int MaxRounds = 99;

    /// <summary>Upper bound for the pause between rounds: 10 s at 60 frames per second.</summary>
    public const int MaxRoundOverFrames = 600;

    /// <summary>Upper bound for the tech window and throw protection: a sanity bound, not a design value.</summary>
    public const int MaxThrowFrames = 99;

    /// <summary>JSON spellings of <see cref="ThrowPriority"/>; match.schema.json lists the same names.</summary>
    public static readonly JsonNames<ThrowPriority> ThrowPriorities = new(
        ("throw", ThrowPriority.Throw),
        ("strike", ThrowPriority.Strike));

    /// <summary>JSON spellings of <see cref="TradeRule"/>; match.schema.json lists the same names.</summary>
    public static readonly JsonNames<TradeRule> TradeRules = new(
        ("trade", TradeRule.Trade),
        ("clash", TradeRule.Clash));
}
