using System.Runtime.CompilerServices;
using Sim.State;

namespace Sim.Modules.Burst;

/// <summary>Both fighters' burst state, indexed by player (0 = P1, 1 = P2).</summary>
[InlineArray(MatchState.PlayerCount)]
public struct BurstFighters
{
    private BurstFighter _element;
}
