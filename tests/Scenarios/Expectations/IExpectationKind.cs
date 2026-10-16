namespace Scenarios.Tests.Expectations;

/// <summary>One kind of scenario expectation, chosen by its keyword: <c>expect [p1|p2] &lt;keyword&gt; …</c>.</summary>
/// <remarks>
/// Implementations are found by reflection (<see cref="ExpectationKinds"/>): adding a kind is adding one class with a
/// public parameterless constructor, nothing else. For a single integer value, derive from <see cref="ValueKind"/>.
/// </remarks>
public interface IExpectationKind
{
    /// <summary>Lowercase word after <c>expect</c> (and the player), e.g. <c>hitstop</c>; unique across kinds.</summary>
    string Keyword { get; }

    /// <summary>True: about one fighter, written with <c>p1</c>/<c>p2</c>. False: about the whole match.</summary>
    bool IsPerFighter { get; }

    /// <summary>Reads the words after the keyword; throws <see cref="ExpectationSyntax.Error"/> when they don't fit.</summary>
    IExpectation Parse(ExpectationSyntax syntax);
}
