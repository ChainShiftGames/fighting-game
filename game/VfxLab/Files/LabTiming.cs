namespace FightingGame.VfxLab.Files;

/// <summary>
/// The move's timing and layout the effect plays in, copied from its data: frames per part and world pixels
/// (70 per metre), from the caster's feet, facing right.
/// </summary>
/// <param name="ChargeFrames">The startup before the projectile appears (its spawn frame − 1).</param>
/// <param name="RestFrames">Empty frames after the impact before the loop starts again.</param>
/// <param name="SpawnX">Where the projectile's hitbox centre is on its first frame.</param>
/// <param name="Speed">How far it flies a frame.</param>
public sealed record LabTiming(
    float ChargeFrames = 12f,
    float FlightFrames = 60f,
    float ImpactFrames = 30f,
    float RestFrames = 20f,
    float SpawnX = 45f,
    float SpawnY = 70f,
    float Speed = 6f,
    float HitboxSize = 30f,
    float FighterWidth = 55f,
    float FighterHeight = 115f)
{
    public int Charge => System.Math.Max(0, (int)ChargeFrames);

    public int Flight => System.Math.Max(1, (int)FlightFrames);

    public int Impact => System.Math.Max(1, (int)ImpactFrames);

    public int Rest => System.Math.Max(0, (int)RestFrames);
}
