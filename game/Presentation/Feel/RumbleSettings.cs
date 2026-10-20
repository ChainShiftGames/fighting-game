namespace FightingGame.Presentation.Feel;

/// <summary>Controller rumble pulses on contact (developer settings, presentation).</summary>
/// <param name="HeavyStrength">Strength (0 to 1) for who got hit, and for the loser on a knock-out.</param>
/// <param name="LightStrength">Strength (0 to 1) for the attacker and on block.</param>
/// <param name="HitSeconds">How long a hit's or block's pulse lasts.</param>
/// <param name="KnockOutSeconds">How long the knock-out pulse lasts.</param>
public sealed record RumbleSettings(float HeavyStrength, float LightStrength, float HitSeconds, float KnockOutSeconds);
