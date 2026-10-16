using Scenarios.Tests.Runner;
using Xunit;

namespace Scenarios.Tests.RunnerTests;

/// <summary>Invalid scenarios fail loudly with the file, the line and a hint, never silently pass.</summary>
public sealed class ScenarioParserTests
{
    [Fact]
    public void UnknownExpectationListsTheKnownKinds()
    {
        string message = SyntaxError("frame 1: expect p1 hitstn = 3");

        Assert.StartsWith($"{InlineScenario.File}:3: can't expect 'hitstn'", message);
        Assert.Contains("hitstop", message);
    }

    [Fact]
    public void MotionInOneInputPointsToTheSequenceSyntax()
    {
        string message = SyntaxError("frame 1: p1 236c", "frame 3: expect p1 x < 0");

        Assert.Contains("'frame 1: p1 2, 3, 6c'", message);
    }

    [Fact]
    public void UnknownButtonListsTheButtons()
    {
        string message = SyntaxError("frame 1: p1 5x", "frame 1: expect p1 x < 0");

        Assert.Contains("buttons: a, b, c, d, special, dash", message);
    }

    [Fact]
    public void SetupAfterFrameLinesIsRejected()
    {
        string message = SyntaxError("frame 1: expect p1 x < 0", "setup: p1 x = 0");

        Assert.StartsWith($"{InlineScenario.File}:4: setup lines must come before", message);
    }

    [Fact]
    public void TwoInputsForOnePlayerOnOneFrameAreRejected()
    {
        string message = SyntaxError("frames 1-3: p1 6", "frame 2: p1 a", "frame 3: expect p1 x < 0");

        Assert.Equal($"{InlineScenario.File}:4: p1 already has an input on frame 2 (line 3)", message);
    }

    [Fact]
    public void ScenarioWithoutExpectationsIsRejected()
    {
        string message = SyntaxError("frame 1: p1 6");

        Assert.Contains("the scenario checks nothing", message);
    }

    [Fact]
    public void MissingCharacterIsRejected()
    {
        var exception = Assert.Throws<ScenarioSyntaxException>(() => InlineScenario.ParseRaw(
            "setup: p1 character scenario_dummy\nframe 1: expect p1 x < 0"));

        Assert.Contains("no character for p2; add 'setup: p2 character <id>'", exception.Message);
    }

    [Fact]
    public void UnknownCharacterListsTheKnownOnes()
    {
        var exception = Assert.Throws<ScenarioSyntaxException>(() => InlineScenario.ParseRaw(
            "setup: p1 character nobody"));

        Assert.Contains("no character 'nobody'; known: scenario_dummy", exception.Message);
    }

    [Fact]
    public void FighterKindNeedsAPlayerAndMatchKindMustNotHaveOne()
    {
        Assert.Contains("write 'p1 x'", SyntaxError("frame 1: expect x = 0"));
        Assert.Contains("drop the player", SyntaxError("frame 1: expect p1 freeze = 0"));
    }

    [Fact]
    public void OrderingComparisonOnFacingIsRejected()
    {
        Assert.Contains("use = or !=", SyntaxError("frame 1: expect p1 facing < right"));
    }

    [Fact]
    public void ValueThatCantBeSetIsRejectedInSetup()
    {
        Assert.Contains("can't set 'damage' in setup", SyntaxError("setup: p1 damage = 5"));
    }

    [Fact]
    public void ModuleExpectationNeedsTheModuleEnabled()
    {
        Assert.Contains(
            "add 'setup: module contract_probe'", SyntaxError("frame 1: expect module contract_probe[0] = 1"));
    }

    [Fact]
    public void TradeRuleSetupOverridesTheRuleFromData()
    {
        Scenario scenario = InlineScenario.Parse("setup: trade clash", "frame 1: expect p1 x < 0");

        Assert.Equal(Sim.Rules.TradeRule.Clash, scenario.Setup.BuildRules().TradeRule);
    }

    [Fact]
    public void UnknownTradeRuleListsTheRules()
    {
        Assert.Contains("trade, clash", SyntaxError("setup: trade p1_wins"));
        Assert.Contains("no player here", SyntaxError("setup: p1 trade clash"));
    }

    [Fact]
    public void CaseAndCommentsAreIgnored()
    {
        Scenario scenario = InlineScenario.Parse(
            "# a comment", "FRAME 2: P1 6A   # hold forward A", "Frame 2: Expect P1 Input = 6A");

        Assert.Equal(InputNotation.Parse("6a"), scenario.Inputs.Get(0, 2));
    }

    private static string SyntaxError(params string[] lines) =>
        Assert.Throws<ScenarioSyntaxException>(() => InlineScenario.Parse(lines)).Message;
}
