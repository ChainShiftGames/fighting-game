using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>timer</c>: round timer frames left.</summary>
public sealed class TimerKind : ValueKind, ISettableKind
{
    public override string Keyword => "timer";

    public override bool IsPerFighter => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Round.TimerFramesLeft;

    public void Set(ref MatchState state, int player, int value) => state.Round.TimerFramesLeft = value;
}
