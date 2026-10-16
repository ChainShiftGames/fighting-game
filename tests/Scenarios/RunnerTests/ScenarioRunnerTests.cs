using Scenarios.Tests.Runner;
using Xunit;

namespace Scenarios.Tests.RunnerTests;

/// <summary>The runner checks every frame of a range and reports the first failure with the actual state.</summary>
public sealed class ScenarioRunnerTests
{
    [Fact]
    public void FailureNamesTheLineFrameExpectedAndActualValue()
    {
        ScenarioResult result = InlineScenario.Run(
            "setup: p2 hitstop = 3",
            "frame 2: expect p2 hitstop = 5");

        Assert.False(result.Passed);
        Assert.StartsWith(
            $"{InlineScenario.File}:4: frame 2: expected p2 hitstop = 5, but it was 1", result.Report);
    }

    [Fact]
    public void FailureReportListsTheActualStateOfBothFightersAndTheMatch()
    {
        ScenarioResult result = InlineScenario.Run(
            "setup: p1 x = -1234",
            "frame 1: expect p1 x = 0");

        Assert.Contains("State after frame 1:", result.Report);
        Assert.Contains("p1: damage = 0, facing = right, health = 10000, hitstop = 0, input = 5, move = idle, " +
            "move_frame = 2, state = idle, stun = 0, vx = 0, vy = 0, x = -1234, y = 0", result.Report);
        Assert.Contains("p2: ", result.Report);
        Assert.Contains("match: freeze = 0, round = 1, timer = 0", result.Report);
    }

    [Fact]
    public void OnlyTheFirstFailureIsReported()
    {
        ScenarioResult result = InlineScenario.Run(
            "frame 3: expect p1 hitstop = 9",
            "frame 2: expect p1 health = 1");

        Assert.StartsWith($"{InlineScenario.File}:4: frame 2:", result.Report);
        Assert.DoesNotContain("frame 3", result.Report);
    }

    [Fact]
    public void RangedExpectationIsCheckedOnEveryFrame()
    {
        ScenarioResult result = InlineScenario.Run(
            "setup: p1 hitstop = 3",
            "frames 1-5: expect p1 hitstop > 0");

        Assert.StartsWith($"{InlineScenario.File}:4: frame 3: expected p1 hitstop > 0, but it was 0", result.Report);
    }

    [Fact]
    public void OrderedComparisonsPass()
    {
        ScenarioResult result = InlineScenario.Run(
            "frame 1: expect p1 x < 0",
            "frame 1: expect p2 x >= 150000",
            "frame 1: expect p2 x <= 150000",
            "frame 1: expect p1 health != 0");

        Assert.True(result.Passed, result.Report);
    }

    [Fact]
    public void RunLastsUntilTheLastInputOrExpectation()
    {
        Scenario scenario = InlineScenario.Parse(
            "frame 3: expect p1 x < 0",
            "frames 5-7: p2 6");

        Assert.Equal(7, scenario.LastFrame);
    }
}
