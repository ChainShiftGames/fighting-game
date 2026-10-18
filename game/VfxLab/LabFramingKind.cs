namespace FightingGame.VfxLab;

/// <summary>How close the Lab's camera is.</summary>
public enum LabFramingKind
{
    /// <summary>The whole cast, caster to target, at least as wide as the match camera shows.</summary>
    Match,

    /// <summary>Follows the effect, a bit over twice the match's size.</summary>
    CloseUp,

    /// <summary>Follows the effect, five times the match's size, to judge detail.</summary>
    Extreme,
}
