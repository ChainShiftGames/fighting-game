namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p2 damage</c>: health lost so far, i.e. the character's stats.health minus current health.</summary>
public sealed class DamageKind : ValueKind
{
    public override string Keyword => "damage";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) =>
        frame.Character(player).Stats.Health - frame.After.Fighters[player].Health;
}
