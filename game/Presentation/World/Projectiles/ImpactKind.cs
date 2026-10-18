namespace FightingGame.Presentation.World.Projectiles;

/// <summary>How a projectile ended, which picks its impact's look.</summary>
public enum ImpactKind
{
    Hit = 0,
    Block = 1,
    Clash = 2,
    Fizzle = 3,
}
