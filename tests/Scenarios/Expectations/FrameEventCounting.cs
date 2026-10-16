using System;
using System.Globalization;
using Sim.Events;

namespace Scenarios.Tests.Expectations;

/// <summary>
/// The shared part of the event kinds: reads <c>&lt;kind&gt; [&lt;comparison&gt; &lt;count&gt;]</c> and counts the frame's
/// events of that kind for one owner (a fighter, or <see cref="FrameEvent.NoFighter"/> for match events).
/// </summary>
public static class FrameEventCounting
{
    /// <param name="owner">The fighter the events must belong to, or <see cref="FrameEvent.NoFighter"/>.</param>
    /// <param name="usage">How to write the expectation, for the error when the arguments don't fit.</param>
    public static IExpectation Parse(ExpectationSyntax syntax, int owner, string usage)
    {
        if (syntax.Arguments.Count is not (1 or 3))
        {
            throw syntax.Error(usage);
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
        return new ValueExpectation(
            syntax.Text,
            frame => Count(frame, owner, kind),
            count => count.ToString(CultureInfo.InvariantCulture) + " such events",
            comparison,
            expected);
    }

    // Events of the frame just run: after ticking frame F the state's frame counter is F + 1 (FrameEvents).
    private static int Count(ScenarioFrame frame, int owner, FrameEventKind kind)
    {
        int count = 0;
        foreach (FrameEvent frameEvent in frame.After.Events.OfFrame(frame.Before.Frame))
        {
            count += frameEvent.Kind == kind && frameEvent.Fighter == owner ? 1 : 0;
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
