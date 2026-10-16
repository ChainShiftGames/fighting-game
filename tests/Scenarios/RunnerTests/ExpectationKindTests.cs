using Scenarios.Tests.Expectations;
using Scenarios.Tests.Runner;
using Sim.Events;
using Sim.Math;
using Sim.State;
using Xunit;

namespace Scenarios.Tests.RunnerTests;

/// <summary>Kinds whose state no gameplay stage produces yet, checked on hand-built frames.</summary>
public sealed class ExpectationKindTests
{
    private const int Player1 = 0;
    private const int Player2 = 1;

    [Fact]
    public void EventCountsOnlyThatFightersEventsOfThatKindInTheFrameJustRun()
    {
        MatchState before = default;
        before.Frame = 7;
        MatchState after = before;
        after.Events.Emit(7, FrameEventKind.Swing, Player1, IntVector2.Zero, detail: 0);
        after.Events.Emit(7, FrameEventKind.Hit, Player2, IntVector2.Zero, detail: 0);
        after.Frame = 8;
        ScenarioFrame frame = Frame(before, after);

        Assert.True(IsMet("frame 8: expect p1 event swing", frame));
        Assert.False(IsMet("frame 8: expect p1 event hit", frame));
        Assert.True(IsMet("frame 8: expect p2 event hit = 1", frame));
        Assert.True(IsMet("frame 8: expect p2 event landing = 0", frame));
    }

    [Fact]
    public void EventsOfAnEarlierFrameDontCount()
    {
        MatchState before = default;
        before.Events.Emit(3, FrameEventKind.Swing, Player1, IntVector2.Zero, detail: 0);
        before.Frame = 7;
        MatchState after = before;
        after.Frame = 8;

        Assert.True(IsMet("frame 8: expect p1 event swing = 0", Frame(before, after)));
    }

    [Fact]
    public void NamedValuesListTheValidNames()
    {
        Assert.Contains("walk_forward", ParseError("frame 1: expect p1 state = walking"));
        Assert.Contains("moves: 5a, idle, crouch", ParseError("frame 1: expect p1 move = 2b"));
        Assert.Contains("swing", ParseError("frame 1: expect p1 event whiff"));
    }

    private static ScenarioFrame Frame(MatchState before, MatchState after)
    {
        ScenarioFrame frame = new(InlineScenario.Parse("frame 1: expect p1 x < 0").Setup);
        frame.Begin(after.Frame, in before);
        frame.End(in after);
        return frame;
    }

    private static bool IsMet(string line, ScenarioFrame frame) =>
        InlineScenario.Parse(line).Expectations[0].Expectation.IsMet(frame, out _);

    private static string ParseError(string line) =>
        Assert.Throws<ScenarioSyntaxException>(() => InlineScenario.Parse(line)).Message;
}
