namespace Sim.State;

/// <summary>Where the match is: which round, how much time is left, who has won rounds and whether it's over.</summary>
/// <remarks>
/// Owned by the round and timer step (<c>Sim.Match.RoundStage</c>), whose doc comment has the rules; the values it
/// works with come from the match rules (P1-13).
/// </remarks>
public struct RoundState
{
    /// <summary>1 for the first round.</summary>
    public int Round;

    /// <summary>Round timer frames left; time-out on the frame it counts down to 0. Stopped at 0: no time limit.</summary>
    public int TimerFramesLeft;

    public RoundWins Wins;

    public RoundPhase Phase;

    /// <summary>Frames of the round-over pause left after this one; the next round (or match over) follows at 0.</summary>
    public int RoundOverFramesLeft;

    /// <summary>How the latest round ended; None while it is still being fought.</summary>
    public RoundEnding Ending;

    /// <summary>Who won the latest round (both = a draw); none while it is still being fought.</summary>
    public Winners RoundWinners;

    /// <summary>Who won the match once <see cref="Phase"/> is <see cref="RoundPhase.MatchOver"/> (both = a draw).</summary>
    public Winners MatchWinners;
}
