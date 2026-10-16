using Scenarios.Tests.Runner;

namespace Scenarios.Tests.RunnerTests;

/// <summary>Builds scenarios from lines in a test, with both characters already set up (lines 1 and 2).</summary>
internal static class InlineScenario
{
    public const string File = "inline.scenario";

    private const string Characters = "setup: p1 character scenario_dummy\nsetup: p2 character scenario_dummy\n";

    public static Scenario Parse(params string[] lines) => ParseRaw(Characters + string.Join("\n", lines));

    /// <summary>Without the character lines.</summary>
    public static Scenario ParseRaw(string text) => new ScenarioParser(ScenarioData.Shared).Parse(File, text);

    public static ScenarioResult Run(params string[] lines) => ScenarioRunner.Run(Parse(lines));
}
