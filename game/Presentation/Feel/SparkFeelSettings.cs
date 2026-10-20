namespace FightingGame.Presentation.Feel;

/// <summary>
/// How strong each hit spark reads (developer settings, presentation): how hit data maps to a spark's weight, and
/// one size for all sparks. The spark's look itself is tuned in the VFX Lab (ADR-0031 §6).
/// </summary>
/// <param name="LightHitstop">The hitstop of the lightest hits (weight 0).</param>
/// <param name="HeavyHitstop">The hitstop of the heaviest hits (weight 1); above <paramref name="LightHitstop"/>.</param>
/// <param name="CounterWeight">What a counter hit adds to the weight.</param>
/// <param name="BlockShare">The share of the weight a block keeps.</param>
/// <param name="TearFromWeight">The weight from which a hit tears without the art file saying so.</param>
/// <param name="TechWeight">A throw tech's weight (it has no hit of its own).</param>
/// <param name="Scale">Multiplies every spark's size, on top of the art file's per-hit scale.</param>
public sealed record SparkFeelSettings(
    int LightHitstop,
    int HeavyHitstop,
    float CounterWeight,
    float BlockShare,
    float TearFromWeight,
    float TechWeight,
    float Scale);
