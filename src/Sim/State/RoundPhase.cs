namespace Sim.State;

/// <summary>What the match is doing: fighting, pausing after a round ended, or finished.</summary>
public enum RoundPhase
{
    /// <summary>The round is on: players have control and the timer runs.</summary>
    Fighting = 0,

    /// <summary>A KO or time-out ended the round; the logic keeps running with neutral input until the next round.</summary>
    RoundOver = 1,

    /// <summary>The match has a result; the logic keeps running with neutral input and nothing else changes.</summary>
    MatchOver = 2,
}
