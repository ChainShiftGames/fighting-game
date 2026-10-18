using Godot;
using Sim.Math;

namespace FightingGame.Presentation.World;

/// <summary>Places logic positions (subpixels, y up) in the 3D scene: metres on the gameplay plane z = 0 (ADR-0027).</summary>
public static class WorldScale
{
    // Developer setting (presentation): 70 world pixels per metre, so Testman's 120 px standing hurtbox is a 1.71 m
    // fighter and models are authored at real-world size.
    public const float SubpixelsPerMetre = 70_000f;

    public static float ToMetres(float subpixels) => subpixels / SubpixelsPerMetre;

    public static Vector3 ToWorld(IntVector2 position) => new(ToMetres(position.X), ToMetres(position.Y), 0f);

    /// <summary>The middle of <paramref name="box"/> on the gameplay plane.</summary>
    public static Vector3 CentreOf(AxisAlignedBox box) =>
        new(ToMetres((box.MinX + box.MaxX) / 2f), ToMetres((box.MinY + box.MaxY) / 2f), 0f);
}
