namespace Sim.Modules.ComboLimits;

/// <summary>The combo limits module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
/// <remarks>
/// "Earlier hits" are the hits the defender has taken in the current combo before this one, so a combo's first hit is
/// never scaled. Permille values are thousandths. All zero (the module enabled without config): no limits at all,
/// except that a knocked-down defender can't be hit (no OTG hits).
/// </remarks>
public struct ComboLimitsConfig : IModuleConfig
{
    /// <summary>Damage proration: thousandths of a hit's damage taken off per earlier hit.</summary>
    public int DamageProrationPermille;

    /// <summary>The least a prorated hit does, in thousandths of its damage.</summary>
    public int MinimumDamagePermille;

    /// <summary>Hitstun decay: frames of hitstun taken off per earlier hit.</summary>
    public int HitstunDecayFrames;

    /// <summary>The most hitstun decay takes off a hit.</summary>
    public int MaximumHitstunDecayFrames;

    /// <summary>Decay never brings a hit's hitstun below this (a hit with less keeps its own).</summary>
    public int MinimumHitstunFrames;

    /// <summary>Gravity scaling: extra gravity on the defender per hit in the combo, in thousandths of its own.</summary>
    public int GravityScalingPermille;

    /// <summary>The most extra gravity scaling adds, in thousandths of the defender's own.</summary>
    public int MaximumGravityPermille;

    /// <summary>OTG rule: hits a knocked-down defender can take per combo; later ones whiff.</summary>
    public int OtgHits;
}
