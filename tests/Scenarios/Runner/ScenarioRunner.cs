using System.IO;
using Scenarios.Tests.Expectations;
using Sim.Composition;
using Sim.Input;
using Sim.Rules;
using Sim.State;

namespace Scenarios.Tests.Runner;

/// <summary>Runs a scenario frame by frame and stops at the first expectation that doesn't hold.</summary>
/// <remarks>
/// Setup values are written over the composed start state in file order. Each frame: turn both players'
/// facing-relative inputs into absolute words using the facing at the start of the frame, tick, then check that
/// frame's expectations in file order.
/// </remarks>
public static class ScenarioRunner
{
    /// <summary>Parses and runs a file under tests/Scenarios/, e.g. <c>Input/socd.scenario</c>.</summary>
    /// <exception cref="ScenarioSyntaxException">The file can't be read as a scenario.</exception>
    public static ScenarioResult RunFile(string file)
    {
        string text = File.ReadAllText(ScenarioPaths.FullPath(file));
        return Run(new ScenarioParser(ScenarioData.Shared).Parse(file, text));
    }

    public static ScenarioResult Run(Scenario scenario)
    {
        ScenarioSetup setup = scenario.Setup;
        MatchRules rules = setup.BuildRules();
        MatchSimulation match = new(
            setup.Character(0),
            setup.Character(1),
            setup.Buttons,
            setup.Motions.Motions,
            setup.Motions.Movement,
            in rules,
            setup.Flags);
        MatchState state = match.StartState();
        foreach (SetupValue value in setup.Values)
        {
            value.Kind.Set(ref state, value.Player, value.Value);
        }

        ScenarioFrame frame = new(scenario.Setup);
        for (int number = 1; number <= scenario.LastFrame; number++)
        {
            InputWord player1 = InputFor(scenario, in state, 0, number);
            InputWord player2 = InputFor(scenario, in state, 1, number);
            frame.Begin(number, in state);
            match.Tick(ref state, player1, player2);
            frame.End(in state);

            foreach (FrameExpectation expectation in scenario.Expectations)
            {
                if (expectation.Frames.Contains(number) && !expectation.Expectation.IsMet(frame, out string actual))
                {
                    return Failure(expectation, frame, actual);
                }
            }
        }

        return ScenarioResult.Pass(scenario.File);
    }

    private static InputWord InputFor(Scenario scenario, in MatchState state, int player, int frame) =>
        InputNotation.ForFacing(scenario.Inputs.Get(player, frame), state.Fighters[player].Facing);

    private static ScenarioResult Failure(FrameExpectation expectation, ScenarioFrame frame, string actual)
    {
        string report =
            $"{expectation.Location}: frame {frame.Number}: expected {expectation.Expectation.Description}, " +
            $"but it was {actual}\n" +
            $"State after frame {frame.Number}:\n{StateReport.Describe(frame)}";
        return new ScenarioResult(false, report);
    }
}
