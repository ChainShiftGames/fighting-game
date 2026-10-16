using Scenarios.Tests.Runner;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 entity_x</c>: x of the fighter's oldest entity still out, or <c>none</c>.</summary>
public sealed class EntityXKind : ValueKind
{
    public override string Keyword => "entity_x";

    public override bool IsPerFighter => true;

    public override int Read(ScenarioFrame frame, int player) =>
        OwnedEntities.ReadFirst(in frame.After, player, entity => entity.Position.X);

    public override int ParseValue(string text, ScenarioSetup setup, int player) => OwnedEntities.Parse(text, Keyword);

    public override string FormatValue(int value, ScenarioSetup setup, int player) => OwnedEntities.Format(value);
}
