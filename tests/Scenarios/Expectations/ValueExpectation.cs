using System;

namespace Scenarios.Tests.Expectations;

/// <summary>Compares one integer read from the frame with an expected value, e.g. <c>p2 x = 120000</c>.</summary>
public sealed class ValueExpectation : IExpectation
{
    private readonly Func<ScenarioFrame, int> _read;
    private readonly Func<int, string> _format;
    private readonly Comparison _comparison;
    private readonly int _expected;

    public ValueExpectation(
        string description, Func<ScenarioFrame, int> read, Func<int, string> format, Comparison comparison, int expected)
    {
        Description = description;
        _read = read;
        _format = format;
        _comparison = comparison;
        _expected = expected;
    }

    public string Description { get; }

    public bool IsMet(ScenarioFrame frame, out string actual)
    {
        int value = _read(frame);
        actual = _format(value);
        return _comparison.Holds(value, _expected);
    }
}
