namespace Sim.Data.Rules;

/// <summary>The special button shortcut's trade-off in <c>rules/match.json</c> (developer settings, ADR-0020).</summary>
/// <param name="DamagePercent">What a shortcut move's hits deal, as a percentage of the hit's damage (100 = full).</param>
/// <param name="CooldownFrames">Fighter frames after a shortcut move starts before the next shortcut works.</param>
public sealed record ShortcutFormat(int DamagePercent, int CooldownFrames);
