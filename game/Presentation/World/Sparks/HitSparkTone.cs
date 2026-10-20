namespace FightingGame.Presentation.World.Sparks;

/// <summary>
/// A hit spark's colours, by what the contact was. The values are the shader's <c>kind</c> uniform and follow
/// <c>ImpactKind</c> for the first three, so the VFX Lab's impact kinds show them (its fizzle shows the counter tone).
/// </summary>
public enum HitSparkTone
{
    Hit = 0,
    Block = 1,

    /// <summary>A clash or a throw tech: neither side got hit.</summary>
    Clash = 2,

    /// <summary>A counter hit or punish counter.</summary>
    Counter = 3,
}
