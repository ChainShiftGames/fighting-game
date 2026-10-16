namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 move_frame</c>: frame of the playing move, 1 on the frame it started (as in frame data).</summary>
public sealed class MoveFrameKind : ValueKind
{
    public override string Keyword => "move_frame";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].MoveFrame;
}
