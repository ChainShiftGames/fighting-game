using Sim.Characters;
using Sim.Rules;

namespace Sim.Data.Rules;

/// <summary>The loaded <c>rules/match.json</c>: match-wide gameplay values (developer settings, ARCHITECTURE.md §6).</summary>
/// <remarks>Module order and config come later.</remarks>
public sealed class MatchRulesFile
{
    public MatchRulesFile(
        int roundStartDistance,
        int wallDistance,
        int maxPlayerDistance,
        TradeRule tradeRule,
        RoundFormat rounds,
        int entitiesPerPlayer,
        CounterBonus counterHit,
        CounterBonus punishCounter)
    {
        RoundStartDistance = roundStartDistance;
        WallDistance = wallDistance;
        MaxPlayerDistance = maxPlayerDistance;
        TradeRule = tradeRule;
        Rounds = rounds;
        EntitiesPerPlayer = entitiesPerPlayer;
        CounterHit = counterHit;
        PunishCounter = punishCounter;
    }

    /// <summary>Subpixels from the stage centre.</summary>
    public int RoundStartDistance { get; }

    /// <summary>Subpixels from the stage centre to each wall.</summary>
    public int WallDistance { get; }

    /// <summary>Subpixels from one fighter's outer pushbox edge to the other's, at most: the logic camera's width.</summary>
    public int MaxPlayerDistance { get; }

    /// <summary>What happens when both fighters' attacks connect on the same frame.</summary>
    public TradeRule TradeRule { get; }

    /// <summary>Timer, rounds to win, round limit and the pause between rounds.</summary>
    public RoundFormat Rounds { get; }

    /// <summary>Entity pool slots per player: projectiles and module objects one player can have out at once.</summary>
    public int EntitiesPerPlayer { get; }

    /// <summary>What a counter hit adds to hits without their own bonus (ADR-0017).</summary>
    public CounterBonus CounterHit { get; }

    /// <summary>What a punish counter adds to hits without their own bonus (ADR-0017).</summary>
    public CounterBonus PunishCounter { get; }

    /// <summary>Copies the values into the match rules, where the logic reads them.</summary>
    public void ApplyTo(ref MatchRules rules)
    {
        rules.RoundStartDistance = RoundStartDistance;
        rules.WallDistance = WallDistance;
        rules.MaxPlayerDistance = MaxPlayerDistance;
        rules.TradeRule = TradeRule;
        rules.RoundTimerFrames = Rounds.TimerFrames;
        rules.RoundsToWin = Rounds.RoundsToWin;
        rules.MaxRounds = Rounds.MaxRounds;
        rules.RoundOverFrames = Rounds.RoundOverFrames;
        rules.EntitiesPerPlayer = EntitiesPerPlayer;
        rules.CounterHit = CounterHit;
        rules.PunishCounter = PunishCounter;
    }
}
