namespace Scenarios.Tests.Expectations;

/// <summary>One parsed expectation, checked after every frame it applies to.</summary>
public interface IExpectation
{
    /// <summary>The expectation as written, e.g. <c>p2 x = 120000</c>, for the failure report.</summary>
    string Description { get; }

    /// <summary>True when it holds after this frame; otherwise <paramref name="actual"/> says what was found.</summary>
    bool IsMet(ScenarioFrame frame, out string actual);
}
