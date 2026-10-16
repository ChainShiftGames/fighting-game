using System.Globalization;
using Scenarios.Tests.Runner;
using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>round_phase</c>: <c>fighting</c>, <c>round_over</c> (the pause after a KO or time-out) or <c>match_over</c>.</summary>
public sealed class RoundPhaseKind : ValueKind
{
    public override string Keyword => "round_phase";

    public override bool IsPerFighter => false;

    public override bool IsOrdered => false;

    public override int Read(ScenarioFrame frame, int player) => (int)frame.After.Round.Phase;

    public override int ParseValue(string text, ScenarioSetup setup, int player) =>
        (int)SnakeCaseNames.Parse<RoundPhase>(text, "round phase");

    public override string FormatValue(int value, ScenarioSetup setup, int player) =>
        System.Enum.IsDefined((RoundPhase)value)
            ? SnakeCaseNames.Of((RoundPhase)value)
            : value.ToString(CultureInfo.InvariantCulture);
}
