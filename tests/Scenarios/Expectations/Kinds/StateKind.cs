using System.Globalization;
using Scenarios.Tests.Runner;
using Sim.Fighters.States;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p2 state</c>: what the fighter is doing (<c>idle</c>, <c>crouch</c>, <c>hitstun</c>, <c>attack</c>…).</summary>
public sealed class StateKind : ValueKind
{
    public override string Keyword => "state";

    public override bool IsPerFighter => true;

    public override bool IsOrdered => false;

    public override int Read(ScenarioFrame frame, int player) => (int)frame.After.Fighters[player].Action;

    public override int ParseValue(string text, ScenarioSetup setup, int player) =>
        (int)SnakeCaseNames.Parse<FighterAction>(text, "fighter state");

    public override string FormatValue(int value, ScenarioSetup setup, int player) =>
        System.Enum.IsDefined((FighterAction)value)
            ? SnakeCaseNames.Of((FighterAction)value)
            : value.ToString(CultureInfo.InvariantCulture);
}
