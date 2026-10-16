namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p2 stun</c>: hitstun or blockstun frames left.</summary>
public sealed class StunKind : ValueKind
{
    public override string Keyword => "stun";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].StunFramesLeft;
}
