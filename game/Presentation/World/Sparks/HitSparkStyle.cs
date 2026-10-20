namespace FightingGame.Presentation.World.Sparks;

/// <summary>The shape of a hit spark; the values are the shader's <c>style</c> uniform.</summary>
public enum HitSparkStyle
{
    /// <summary>A star-shaped burst at the contact point: light hits.</summary>
    Burst = 0,

    /// <summary>A long streak along the attack's direction, through and behind the defender: heavy hits, specials.</summary>
    Tear = 1,
}
