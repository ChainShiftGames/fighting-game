using Sim.Math;

namespace FightingGame.Presentation.World.Sparks;

/// <summary>Everything the spark art needs to draw one hit spark, resolved from its event, hit data and art file.</summary>
/// <param name="Weight">How strong the hit is: 0 a light jab, 1 the heaviest hits, a little more on counter hit.</param>
/// <param name="Angle">The way the attack travels on screen, in radians: 0 right, counter-clockwise (facing applied).</param>
/// <param name="Position">Where the spark is centred, in subpixels (offset applied).</param>
/// <param name="Seed">0 to 1, from the event's id: varies the spark's details, the same every time it is shown.</param>
/// <param name="Scale">Multiplies the spark's size.</param>
public readonly record struct HitSparkLook(
    HitSparkStyle Style,
    HitSparkTone Tone,
    float Weight,
    float Angle,
    IntVector2 Position,
    float Seed,
    float Scale);
