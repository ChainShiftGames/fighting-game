namespace FightingGame.Presentation.World.Projectiles;

/// <summary>How a move's projectiles are drawn, from the character's art file: effect names in <see cref="ProjectileEffects"/>.</summary>
/// <param name="Flight">The projectile while it exists.</param>
/// <param name="Charge">Shown at the spawn point during the move's startup, before the projectile appears; null for none.</param>
/// <param name="Impact">Shown where it hits, is blocked, clashes or runs out; null leaves the plain hit spark.</param>
public sealed record ProjectileArtSettings(string Flight, string? Charge = null, string? Impact = null);
