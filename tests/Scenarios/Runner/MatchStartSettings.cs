namespace Scenarios.Tests.Runner;

/// <summary>Where a round starts; gameplay-affecting (developer setting), so it belongs in data or MatchRules.</summary>
/// <remarks>
/// Neither has a field for it yet (round start is P1-13): until then the runner supplies
/// <see cref="ScenarioStartSettings.Default"/>. Distance in subpixels.
/// </remarks>
public readonly record struct MatchStartSettings(int StartDistanceFromCentre);
