using Sim.Rules;
using Sim.State;

namespace Sim.Match;

/// <summary>Round wins and the match result: who gets a win for a round, and when the match is decided.</summary>
/// <remarks>
/// - Each round winner gets a round win. A draw (double KO, equal health share at time-out) gives both players one,
///   so a player one win from the match wins it with a draw.
/// - Exception: when a draw would give both players their last win at once, nobody gets one and an extra deciding
///   round is played instead, since both reaching the win count names no winner.
/// - The match is decided as soon as a player has <see cref="MatchRules.RoundsToWin"/> wins. Still undecided after
///   round <see cref="MatchRules.MaxRounds"/> (only reachable through extra rounds after draws), it is a draw, so
///   even two idle players finish a match.
/// </remarks>
public sealed class MatchScore
{
    private readonly int _roundsToWin;
    private readonly int _maxRounds;

    public MatchScore(in MatchRules rules)
    {
        _roundsToWin = rules.RoundsToWin;
        _maxRounds = rules.MaxRounds;
    }

    /// <summary>Gives the round's winners their round wins.</summary>
    public void Award(ref RoundState round, Winners roundWinners)
    {
        if (roundWinners.IsDraw && IsOneWinAway(in round, 0) && IsOneWinAway(in round, 1))
        {
            return;
        }

        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            round.Wins[player] += roundWinners.Includes(player) ? 1 : 0;
        }
    }

    /// <summary>The match winners once the latest round's wins are in; none while another round is due.</summary>
    public Winners Result(in RoundState round)
    {
        Winners reached = Winners.None;
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            reached.Players |= round.Wins[player] >= _roundsToWin ? Winners.Only(player).Players : 0;
        }

        if (reached.IsDecided)
        {
            return reached;
        }

        return round.Round >= _maxRounds ? Winners.Both : Winners.None;
    }

    private bool IsOneWinAway(in RoundState round, int player) => round.Wins[player] + 1 >= _roundsToWin;
}
