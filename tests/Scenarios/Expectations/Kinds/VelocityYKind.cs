using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 vy</c>: vertical velocity in subpixels per frame, + is up.</summary>
public sealed class VelocityYKind : ValueKind, ISettableKind
{
    public override string Keyword => "vy";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Velocity.Y;

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Velocity = state.Fighters[player].Velocity with { Y = value };
}
