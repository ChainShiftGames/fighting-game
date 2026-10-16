namespace Scenarios.Tests.Runner;

/// <summary>A line in a scenario file, for error and failure messages (<c>file:line</c>).</summary>
public readonly record struct ScenarioLocation(string File, int Line)
{
    public override string ToString() => Line > 0 ? $"{File}:{Line}" : File;
}
