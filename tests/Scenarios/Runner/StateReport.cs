using System.Collections.Generic;
using System.Linq;
using System.Text;
using Scenarios.Tests.Expectations;
using Sim.State;

namespace Scenarios.Tests.Runner;

/// <summary>The actual state after a frame, one line per fighter and one for the match, from every value kind.</summary>
/// <remarks>Built from <see cref="ExpectationKinds.Values"/>, so a new value kind appears here without changes.</remarks>
public static class StateReport
{
    public static string Describe(ScenarioFrame frame)
    {
        StringBuilder report = new();
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            report.Append("  ").Append(ScenarioPlayers.Label(player)).Append(": ")
                .AppendLine(Values(frame, player, perFighter: true));
        }

        report.Append("  match: ").Append(Values(frame, ScenarioPlayers.None, perFighter: false));
        return report.ToString();
    }

    private static string Values(ScenarioFrame frame, int player, bool perFighter)
    {
        IEnumerable<string> values = ExpectationKinds.Values
            .Where(kind => kind.IsPerFighter == perFighter)
            .Select(kind => $"{kind.Keyword} = {kind.FormatValue(kind.Read(frame, player), frame.Setup, player)}");
        return string.Join(", ", values);
    }
}
