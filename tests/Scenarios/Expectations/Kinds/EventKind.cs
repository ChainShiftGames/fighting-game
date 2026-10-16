using System;
using System.Globalization;
using Sim.Events;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary>
/// <c>p1 event &lt;kind&gt;</c>: the fighter had a frame event of that kind this frame (P1-14), e.g.
/// <c>expect p1 event swing</c>; with a count, <c>expect p2 event hit = 0</c> checks how many.
/// </summary>
public sealed class EventKind : IExpectationKind
{
    public string Keyword => "event";

    public bool IsPerFighter => true;

    public IExpectation Parse(ExpectationSyntax syntax)
    {
        if (syntax.Arguments.Count is not (1 or 3))
        {
            throw syntax.Error("write 'p1 event <kind>' or 'p1 event <kind> <comparison> <count>'");
        }

        FrameEventKind kind;
        try
        {
            kind = SnakeCaseNames.Parse(syntax.Arguments[0], "frame event kind", FrameEventKind.None);
        }
        catch (FormatException exception)
        {
            throw syntax.Error(exception.Message);
        }

        (Comparison comparison, int expected) = syntax.Arguments.Count == 1
            ? (new Comparison(">="), 1)
            : ParseCount(syntax);
        int player = syntax.Player;
        return new ValueExpectation(
            syntax.Text,
            frame => Count(frame, player, kind),
            count => count.ToString(CultureInfo.InvariantCulture) + " such events",
            comparison,
            expected);
    }

    // Events of the frame just run: after ticking frame F the state's frame counter is F + 1 (FrameEvents).
    private static int Count(ScenarioFrame frame, int player, FrameEventKind kind)
    {
        int count = 0;
        foreach (FrameEvent frameEvent in frame.After.Events.OfFrame(frame.Before.Frame))
        {
            count += frameEvent.Kind == kind && frameEvent.Fighter == player ? 1 : 0;
        }

        return count;
    }

    private static (Comparison, int) ParseCount(ExpectationSyntax syntax)
    {
        (Comparison comparison, string text) = syntax.ComparisonAt(1, isOrdered: true);
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int count)
            ? (comparison, count)
            : throw syntax.Error($"'{text}' isn't an event count");
    }
}
