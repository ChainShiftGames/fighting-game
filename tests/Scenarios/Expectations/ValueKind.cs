using System;
using System.Globalization;
using Scenarios.Tests.Runner;

namespace Scenarios.Tests.Expectations;

/// <summary>
/// An expectation kind for one integer of a fighter or the match: <c>expect p2 hitstop = 3</c>, <c>expect freeze &gt; 0</c>.
/// </summary>
/// <remarks>
/// A new value is a small subclass: <see cref="Keyword"/>, <see cref="IsPerFighter"/> and <see cref="Read"/>;
/// override <see cref="ParseValue"/>/<see cref="FormatValue"/> for named values (facing, move ids), set
/// <see cref="IsOrdered"/> false when only = and != make sense, and implement <see cref="ISettableKind"/> to allow
/// it in setup. Every value kind also shows up in the state report of a failing scenario.
/// </remarks>
public abstract class ValueKind : IExpectationKind
{
    public abstract string Keyword { get; }

    public abstract bool IsPerFighter { get; }

    public virtual bool IsOrdered => true;

    /// <param name="player">The fighter; ignore it for a match kind.</param>
    public abstract int Read(ScenarioFrame frame, int player);

    /// <param name="setup">The scenario's setup, e.g. for the player's character.</param>
    /// <exception cref="FormatException">Not a valid value; the message says why.</exception>
    public virtual int ParseValue(string text, ScenarioSetup setup, int player) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new FormatException($"'{text}' isn't an integer ('{Keyword}' is an integer)");

    public virtual string FormatValue(int value, ScenarioSetup setup, int player) =>
        value.ToString(CultureInfo.InvariantCulture);

    public IExpectation Parse(ExpectationSyntax syntax)
    {
        (Comparison comparison, string valueText) = syntax.ComparisonAt(0, IsOrdered);
        int expected;
        try
        {
            expected = ParseValue(valueText, syntax.Setup, syntax.Player);
        }
        catch (FormatException exception)
        {
            throw syntax.Error(exception.Message);
        }

        int player = syntax.Player;
        ScenarioSetup setup = syntax.Setup;
        return new ValueExpectation(
            syntax.Text, frame => Read(frame, player), value => FormatValue(value, setup, player), comparison, expected);
    }
}
