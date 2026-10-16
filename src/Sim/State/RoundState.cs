namespace Sim.State;

/// <summary>Where the match is: which round, how much time is left and who has won rounds.</summary>
/// <remarks>Rules for using these (timer length, rounds to win) come from P1-13 and the match rules.</remarks>
public struct RoundState
{
    /// <summary>1 for the first round.</summary>
    public int Round;

    public int TimerFramesLeft;

    public RoundWins Wins;
}
