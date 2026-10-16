using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 vx</c>: horizontal velocity in subpixels per frame, + is right.</summary>
public sealed class VelocityXKind : ValueKind, ISettableKind
{
    public override string Keyword => "vx";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Velocity.X;

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Velocity = state.Fighters[player].Velocity with { X = value };
}
