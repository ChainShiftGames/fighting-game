using Godot;

namespace FightingGame.Presentation.World.Stages.CastleHall;

/// <summary>A torch on an iron bracket: a flickering flame and the warm pool of light round it.</summary>
public static class WallTorch
{
    // Developer settings (presentation), in metres.
    private const float BracketHeight = 0.4f;
    private const float LightRange = 5f;
    private const float LightEnergy = 1.4f;
    private static readonly Vector2 _flameSize = new(0.3f, 0.45f);
    private static readonly Color _lightColor = new(1f, 0.62f, 0.32f);

    public static Node3D At(Vector3 position)
    {
        var torch = new Node3D { Position = position };
        torch.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.12f, BracketHeight, 0.12f) },
            MaterialOverride = HallMaterial.Iron(),
        });
        torch.AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = _flameSize },
            Position = new Vector3(0f, (BracketHeight + _flameSize.Y) / 2f, 0f),
            MaterialOverride = HallMaterial.Of("candle_flame"),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        torch.AddChild(new OmniLight3D
        {
            Position = new Vector3(0f, BracketHeight, 0f),
            OmniRange = LightRange,
            LightEnergy = LightEnergy,
            LightColor = _lightColor,
        });
        return torch;
    }
}
