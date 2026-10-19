using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Blurs the stage from a little behind the gameplay plane on, so the crisp, outlined fighters stand out from a softer
/// backdrop, as in 2.5D anime fighting games. Fighters, projectiles and effects on the plane stay sharp.
/// </summary>
public sealed class BackdropBlur
{
    // Developer settings (presentation), in metres behind the gameplay plane.
    private const float StartBehindPlane = 4f;
    private const float Transition = 12f;
    private const float Amount = 0.025f;

    /// <summary>The camera attributes to give the camera; their far blur follows the camera's distance.</summary>
    public CameraAttributesPractical Attributes { get; } = new()
    {
        DofBlurFarTransition = Transition,
        DofBlurAmount = Amount,
    };

    /// <param name="planeDistance">The camera's distance to the gameplay plane this frame.</param>
    public void Show(bool blurred, float planeDistance)
    {
        Attributes.DofBlurFarEnabled = blurred;
        Attributes.DofBlurFarDistance = planeDistance + StartBehindPlane;
    }
}
