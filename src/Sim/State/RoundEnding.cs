namespace Sim.State;

/// <summary>How a round ended.</summary>
public enum RoundEnding
{
    /// <summary>The round hasn't ended.</summary>
    None = 0,

    /// <summary>One fighter's health reached 0, or both on the same frame (a double KO).</summary>
    KnockOut = 1,

    /// <summary>The round timer ran out.</summary>
    TimeOut = 2,
}
