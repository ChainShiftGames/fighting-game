using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>freeze</c>: frames the global freeze still lasts (no fighter exempt when set up).</summary>
public sealed class FreezeKind : ValueKind, ISettableKind
{
    public override string Keyword => "freeze";

    public override bool IsPerFighter => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Freeze.FramesLeft;

    public void Set(ref MatchState state, int player, int value) => state.Freeze = new FreezeState { FramesLeft = value };
}
