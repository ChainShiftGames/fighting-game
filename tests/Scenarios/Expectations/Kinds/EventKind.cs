namespace Scenarios.Tests.Expectations.Kinds;

/// <summary>
/// <c>p1 event &lt;kind&gt;</c>: the fighter had a frame event of that kind this frame (P1-14), e.g.
/// <c>expect p1 event swing</c>; with a count, <c>expect p2 event hit = 0</c> checks how many.
/// </summary>
public sealed class EventKind : IExpectationKind
{
    public string Keyword => "event";

    public bool IsPerFighter => true;

    public IExpectation Parse(ExpectationSyntax syntax) => FrameEventCounting.Parse(
        syntax, syntax.Player, "write 'p1 event <kind>' or 'p1 event <kind> <comparison> <count>'");
}
