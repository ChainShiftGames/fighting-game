using Scenarios.Tests.Expectations;

namespace Scenarios.Tests.Runner;

/// <summary>One <c>setup: p1 x = -50000</c> line: a start-state value written before frame 1.</summary>
public sealed record SetupValue(ISettableKind Kind, int Player, int Value);
