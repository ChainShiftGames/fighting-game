using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Inks lines inside the fighters' silhouettes (folds, sleeves, overlapping limbs), as in illustrated 2.5D fighters
/// such as E's Laf++: a full-screen pass in front of the camera, limited to the depth of the gameplay plane.
/// </summary>
public sealed class InkLines
{
    // Developer settings (presentation), in metres behind the gameplay plane: lines fade out over this range, so the
    // stage stays painted.
    private const float FadeStartBehindPlane = 1.5f;
    private const float FadeLength = 2f;
    // Far enough in front of the camera to pass its near plane; the shader puts the quad over the whole screen.
    private const float DistanceInFront = 1f;
    private static readonly StringName _inkedDepth = "inked_depth";

    private readonly ShaderMaterial _material = new()
    {
        Shader = GD.Load<Shader>("res://Assets/Shaders/ink_lines.gdshader"),
        // Reading the screen puts the pass among the transparent draws, where it sorts last (nearest the camera) and
        // would paint the pre-transparent screen over the sparks and other effects; first, they draw on top of it.
        RenderPriority = (int)Material.RenderPriorityMin,
    };

    public InkLines()
    {
        Pass = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = Vector2.One, Material = _material },
            // The shader moves the quad over the whole screen, so it must never be culled.
            ExtraCullMargin = 16384f,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Position = new Vector3(0f, 0f, -DistanceInFront),
        };
    }

    /// <summary>The pass to add as a child of the match camera.</summary>
    public MeshInstance3D Pass { get; }

    /// <param name="planeDistance">The camera's distance to the gameplay plane this frame.</param>
    public void Show(bool inked, float planeDistance)
    {
        Pass.Visible = inked;
        float fadeStart = planeDistance + FadeStartBehindPlane;
        _material.SetShaderParameter(_inkedDepth, new Vector2(fadeStart, fadeStart + FadeLength));
    }
}
