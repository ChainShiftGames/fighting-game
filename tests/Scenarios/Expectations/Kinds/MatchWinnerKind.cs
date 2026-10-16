using Scenarios.Tests.Runner;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>match_winner</c>: <c>p1</c>, <c>p2</c> or <c>draw</c> once the match is over; <c>none</c> until then.</summary>
public sealed class MatchWinnerKind : ValueKind
{
    public override string Keyword => "match_winner";

    public override bool IsPerFighter => false;

    public override bool IsOrdered => false;

    public override int Read(ScenarioFrame frame, int player) => frame.After.Round.MatchWinners.Players;

    public override int ParseValue(string text, ScenarioSetup setup, int player) => WinnerNames.Parse(text);

    public override string FormatValue(int value, ScenarioSetup setup, int player) => WinnerNames.Format(value);
}
