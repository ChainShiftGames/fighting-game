using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>Both players' input histories, indexed by player (0 = P1, 1 = P2).</summary>
[InlineArray(MatchState.PlayerCount)]
public struct InputHistories
{
    private InputHistory _element;
}
