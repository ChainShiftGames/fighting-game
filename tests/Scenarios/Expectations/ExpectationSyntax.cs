using System.Collections.Generic;
using Scenarios.Tests.Runner;

namespace Scenarios.Tests.Expectations;

/// <summary>What an <see cref="IExpectationKind"/> gets to parse: the words after its keyword, plus context.</summary>
public sealed class ExpectationSyntax
{
    public ExpectationSyntax(
        string text,
        int player,
        string keyword,
        IReadOnlyList<string> arguments,
        ScenarioSetup setup,
        ScenarioLocation location)
    {
        Text = text;
        Player = player;
        Keyword = keyword;
        Arguments = arguments;
        Setup = setup;
        Location = location;
    }

    /// <summary>The whole expectation after <c>expect</c>, lowercase; a good <see cref="IExpectation.Description"/>.</summary>
    public string Text { get; }

    /// <summary>The fighter, or <see cref="ScenarioPlayers.None"/> for a match kind.</summary>
    public int Player { get; }

    public string Keyword { get; }

    /// <summary>The words after the keyword, e.g. <c>["=", "120000"]</c>.</summary>
    public IReadOnlyList<string> Arguments { get; }

    /// <summary>The scenario's setup (all setup lines come first), e.g. to find an enabled module's slot.</summary>
    public ScenarioSetup Setup { get; }

    public ScenarioLocation Location { get; }

    /// <summary>The exception to throw when the arguments don't fit; the message gets the file and line.</summary>
    public ScenarioSyntaxException Error(string reason) => new(Location, reason);

    /// <summary>Reads the common <c>&lt;comparison&gt; &lt;value&gt;</c> ending, starting at <paramref name="index"/>.</summary>
    public (Comparison Comparison, string Value) ComparisonAt(int index, bool isOrdered)
    {
        if (Arguments.Count != index + 2)
        {
            throw Error($"write '{Text.Split(' ')[0]} … <comparison> <value>' with one of {Comparison.All}");
        }

        if (!Comparison.TryParse(Arguments[index], out Comparison comparison))
        {
            throw Error($"'{Arguments[index]}' isn't a comparison; use one of {Comparison.All}");
        }

        if (comparison.IsOrdering && !isOrdered)
        {
            throw Error($"'{Arguments[index]}' needs an ordered value; use = or !=");
        }

        return (comparison, Arguments[index + 1]);
    }
}
