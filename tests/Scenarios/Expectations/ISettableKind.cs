using Sim.State;

namespace Scenarios.Tests.Expectations;

/// <summary>A <see cref="ValueKind"/> a scenario can also set before frame 1: <c>setup: p1 x = -50000</c>.</summary>
/// <remarks>Setup values are written after the fighters are placed and enter idle, in file order.</remarks>
public interface ISettableKind
{
    /// <param name="player">The fighter, or <c>ScenarioPlayers.None</c> for a match kind.</param>
    void Set(ref MatchState state, int player, int value);
}
