using Sim.State;

namespace FightingGame.UI.Results;

/// <summary>What a finished match's logic says: who won and the rounds each player took. Only read, never changed.</summary>
public readonly record struct MatchResult(MatchOutcome Outcome, int Player1Rounds, int Player2Rounds)
{
    public static MatchResult Unfinished { get; } = new(MatchOutcome.Unfinished, 0, 0);

    public bool IsFinished => Outcome != MatchOutcome.Unfinished;

    /// <summary>The winner's side (0 = P1, 1 = P2); -1 for a draw or no result.</summary>
    public int WinnerSide => Outcome switch
    {
        MatchOutcome.Player1Wins => 0,
        MatchOutcome.Player2Wins => 1,
        _ => -1,
    };

    /// <summary>The result in the match's round state; unfinished while the match isn't over.</summary>
    public static MatchResult From(in RoundState round)
    {
        if (round.Phase != RoundPhase.MatchOver || !round.MatchWinners.IsDecided)
        {
            return Unfinished;
        }

        Winners winners = round.MatchWinners;
        MatchOutcome outcome = winners.IsDraw ? MatchOutcome.Draw
            : winners.Includes(0) ? MatchOutcome.Player1Wins
            : MatchOutcome.Player2Wins;
        return new MatchResult(outcome, round.Wins[0], round.Wins[1]);
    }
}
