using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 x</c>: horizontal position in subpixels (1 px = 1000), + is right.</summary>
public sealed class PositionXKind : ValueKind, ISettableKind
{
    public override string Keyword => "x";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Position.X;

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Position = state.Fighters[player].Position with { X = value };
}
