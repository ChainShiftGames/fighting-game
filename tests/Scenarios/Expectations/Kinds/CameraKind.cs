using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>camera</c>: the logic camera's x, the centre of what the screen shows.</summary>
public sealed class CameraKind : ValueKind, ISettableKind
{
    public override string Keyword => "camera";

    public override bool IsPerFighter => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Camera.X;

    public void Set(ref MatchState state, int player, int value) => state.Camera.X = value;
}
