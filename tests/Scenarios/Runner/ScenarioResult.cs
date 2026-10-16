namespace Scenarios.Tests.Runner;

/// <summary>The outcome of a run: passed, or the first failing expectation with the actual state.</summary>
public sealed record ScenarioResult(bool Passed, string Report)
{
    public static ScenarioResult Pass(string file) => new(true, $"{file}: passed");
}
