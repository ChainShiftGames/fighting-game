using System.Globalization;
using Sim.State;

namespace FightingGame.Presentation;

/// <summary>
/// What the big round banner says, worked out from the round state: KO or time-out while a round is over, the winner
/// once the match is. Empty while a round is being fought.
/// </summary>
public static class RoundBannerText
{
    /// <summary>The banner's large headline; empty for no banner.</summary>
    public static string Headline(in RoundState round) => round.Phase switch
    {
        RoundPhase.RoundOver when round.Ending == RoundEnding.KnockOut => round.RoundWinners.IsDraw ? "DOUBLE K.O." : "K.O.",
        RoundPhase.RoundOver when round.Ending == RoundEnding.TimeOut => "TIME OUT",
        RoundPhase.MatchOver => round.MatchWinners.IsDraw ? "DRAW GAME" : $"{Winner(round.MatchWinners)} WINS",
        _ => string.Empty,
    };

    /// <summary>The smaller line under the headline: who took the round; empty when there is nothing to add.</summary>
    public static string Detail(in RoundState round)
    {
        if (round.Phase != RoundPhase.RoundOver || !round.RoundWinners.IsDecided)
        {
            return string.Empty;
        }

        return round.RoundWinners.IsDraw ? "DRAW" : $"{Winner(round.RoundWinners)} WINS THE ROUND";
    }

    // Players by port until there is a character select: both sides are the same test character for now.
    private static string Winner(Winners winners) => string.Create(
        CultureInfo.InvariantCulture, $"PLAYER {(winners.Includes(0) ? 1 : 2)}");
}
