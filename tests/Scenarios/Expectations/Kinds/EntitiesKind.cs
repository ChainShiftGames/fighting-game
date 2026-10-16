namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 entities</c>: how many projectiles (entities) the fighter has out in the pool.</summary>
public sealed class EntitiesKind : ValueKind
{
    public override string Keyword => "entities";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) => OwnedEntities.Count(in frame.After, player);
}
