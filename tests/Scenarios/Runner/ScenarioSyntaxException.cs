using System;

namespace Scenarios.Tests.Runner;

/// <summary>A scenario file that can't be read; the message names the file, the line and what is wrong.</summary>
public sealed class ScenarioSyntaxException : Exception
{
    public ScenarioSyntaxException(ScenarioLocation location, string reason)
        : base($"{location}: {reason}")
    {
        Location = location;
    }

    public ScenarioLocation Location { get; }
}
