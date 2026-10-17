using Sim.Math;

namespace Sim.Modules.ComboLimits;

/// <summary>The combo limits formulas: integer only, each growing linearly with the combo up to its cap.</summary>
public static class ComboScaling
{
    private const int Permille = 1000;

    /// <summary>Damage after proration for <paramref name="earlierHits"/>, rounded down.</summary>
    public static int Damage(int damage, int earlierHits, in ComboLimitsConfig config)
    {
        int floor = IntMath.Clamp(config.MinimumDamagePermille, 0, Permille);
        int proration = Grown(earlierHits, config.DamageProrationPermille, Permille - floor);
        return IntMath.MulDiv(damage, Permille - proration, Permille);
    }

    /// <summary>Hitstun after decay for <paramref name="earlierHits"/>; never below the minimum, or the hit's own if lower.</summary>
    public static int Hitstun(int hitstun, int earlierHits, in ComboLimitsConfig config)
    {
        int decayed = hitstun - Grown(earlierHits, config.HitstunDecayFrames, config.MaximumHitstunDecayFrames);
        int floor = hitstun < config.MinimumHitstunFrames ? hitstun : config.MinimumHitstunFrames;
        return decayed > floor ? decayed : floor;
    }

    /// <summary>Extra gravity in thousandths once the defender has taken <paramref name="hits"/> in the combo.</summary>
    public static int ExtraGravityPermille(int hits, in ComboLimitsConfig config) =>
        Grown(hits, config.GravityScalingPermille, config.MaximumGravityPermille);

    // count × perStep, capped; in long so a long combo can't overflow past the cap.
    private static int Grown(int count, int perStep, int cap)
    {
        long grown = (long)count * perStep;
        return grown < cap ? (int)grown : cap;
    }
}
