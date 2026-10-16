using Scenarios.Tests.Runner;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>round_winner</c>: who won the latest round, <c>p1</c>, <c>p2</c> or <c>draw</c>; <c>none</c> while it's fought.</summary>
public sealed class RoundWinnerKind : ValueKind
{
    public override string Keyword => "round_winner";

    public override bool IsPerFighter => false;

    public override bool IsOrdered => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Round.RoundWinners.Players;

    public override int ParseValue(string text, ScenarioSetup setup, int player) => WinnerNames.Parse(text);

    public override string FormatValue(int value, ScenarioSetup setup, int player) => WinnerNames.Format(value);
}
