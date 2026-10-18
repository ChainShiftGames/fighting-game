using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>Where things stand in the Lab's scene, in world pixels from the caster's feet, facing right.</summary>
public static class LabLayout
{
    /// <summary>The projectile's hitbox centre on <paramref name="frame"/> of its flight (from 1).</summary>
    public static float FlightX(LabTiming timing, int frame) => timing.SpawnX + (timing.Speed * (frame - 1));

    /// <summary>Where it ends: its centre on the last frame of its flight.</summary>
    public static float ImpactX(LabTiming timing) => FlightX(timing, timing.Flight);

    /// <summary>The target stands with its front against the projectile's hitbox where it ends.</summary>
    public static float TargetX(LabTiming timing) => ImpactX(timing) + (timing.HitboxSize / 2f) + (timing.FighterWidth / 2f);

    /// <summary>The point the effect of <paramref name="moment"/> is drawn round.</summary>
    public static (float X, float Y) EffectPoint(LabTiming timing, LabMoment moment) => moment.Phase switch
    {
        LabPhase.Charge => (timing.SpawnX, timing.SpawnY),
        LabPhase.Flight => (FlightX(timing, moment.PhaseFrame), timing.SpawnY),
        _ => (ImpactX(timing), timing.SpawnY),
    };
}
