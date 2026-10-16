using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>Rounds won so far, indexed by player (0 = P1, 1 = P2).</summary>
[InlineArray(MatchState.PlayerCount)]
public struct RoundWins
{
    private int _element;
}
