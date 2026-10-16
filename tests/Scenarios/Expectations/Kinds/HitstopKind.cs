using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 hitstop</c>: frames of hitstop the fighter still skips.</summary>
public sealed class HitstopKind : ValueKind, ISettableKind
{
    public override string Keyword => "hitstop";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].Timing.HitstopFramesLeft;

    public void Set(ref MatchState state, int player, int value) => state.Fighters[player].Timing.HitstopFramesLeft = value;
}
