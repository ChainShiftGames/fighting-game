using Sim.Math;

namespace Scenarios.Tests.Runner;

/// <summary>The start settings scenarios run with, the one place the runner fixes a gameplay value itself.</summary>
/// <remarks>Missing from data/MatchRules (P1-13 round start); replace with the real setting when it exists.</remarks>
public static class ScenarioStartSettings
{
    /// <summary>Each fighter 150 px from the centre; a scenario's setup lines can move them.</summary>
    public static MatchStartSettings Default { get; } = new(StartDistanceFromCentre: Subpixels.FromPixels(150));
}
