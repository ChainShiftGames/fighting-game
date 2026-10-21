namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p2 combo</c>: hits the fighter has taken in the current true combo (<c>FighterState.ComboHits</c>).</summary>
public sealed class ComboKind : ValueKind
{
    public override string Keyword => "combo";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Fighters[player].ComboHits;
}
