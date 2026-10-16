using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>Both fighters inline, indexed by player (0 = P1, 1 = P2), so the state stays one flat block.</summary>
[InlineArray(MatchState.PlayerCount)]
public struct FighterStates
{
    private FighterState _element;
}
