using Sim.Events;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary>
/// <c>match_event &lt;kind&gt;</c>: a frame event of that kind that belongs to no fighter happened this frame, e.g.
/// <c>expect match_event time_out</c>; with a count, <c>expect match_event round_start = 0</c>.
/// </summary>
public sealed class MatchEventKind : IExpectationKind
{
    public string Keyword => "match_event";

    public bool IsPerFighter => false;

    public IExpectation Parse(ExpectationSyntax syntax) => FrameEventCounting.Parse(
        syntax,
        FrameEvent.NoFighter,
        "write 'match_event <kind>' or 'match_event <kind> <comparison> <count>'");
}
