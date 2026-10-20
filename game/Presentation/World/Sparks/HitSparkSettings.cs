namespace FightingGame.Presentation.World.Sparks;

/// <summary>How one hit's spark is drawn, from the character's art file (developer setting, presentation).</summary>
/// <remarks>Every field is optional; <see cref="HitSparkLooks"/> fills in the defaults from the hit's data.</remarks>
/// <param name="Angle">
/// The way the attack travels, in degrees relative to the attacker's facing: 0 forward, 90 up, -90 down. Null takes
/// the hit's launch direction, or forward without one.
/// </param>
/// <param name="OffsetX">Moves the spark from the contact point, in pixels: forward along the attacker's facing.</param>
/// <param name="OffsetY">Moves the spark from the contact point, in pixels: up.</param>
/// <param name="Style">Burst or tear; null picks from the hit's strength and the move's category.</param>
/// <param name="Scale">Multiplies the spark's size.</param>
public sealed record HitSparkSettings(
    float? Angle = null,
    float OffsetX = 0f,
    float OffsetY = 0f,
    HitSparkStyle? Style = null,
    float Scale = 1f);
