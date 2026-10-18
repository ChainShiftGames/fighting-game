using Godot;

namespace FightingGame.Presentation.World.Stages.CastleHall;

/// <summary>An iron chandelier of two candle rings, hanging on a chain from the ceiling, with its own warm light.</summary>
public static class Chandelier
{
    // Developer settings (presentation), in metres: each ring's radius, height above the lowest ring and candle count.
    private static readonly (float Radius, float Height, int Candles)[] _rings = [(1f, 0f, 10), (0.55f, 0.55f, 6)];
    private const float CandleHeight = 0.18f;
    private const float LightRange = 7f;
    private const float LightEnergy = 1.6f;
    private static readonly Vector2 _flameSize = new(0.12f, 0.2f);
    private static readonly Color _candleColor = new(0.92f, 0.86f, 0.72f);
    private static readonly Color _lightColor = new(1f, 0.7f, 0.4f);

    /// <param name="hook">Where the lowest ring hangs.</param>
    /// <param name="chainLength">From the lowest ring up to the ceiling.</param>
    public static Node3D At(Vector3 hook, float chainLength)
    {
        var chandelier = new Node3D { Position = hook };
        StandardMaterial3D iron = HallMaterial.Iron();
        ShaderMaterial flame = HallMaterial.Of("candle_flame");
        var candle = new StandardMaterial3D { AlbedoColor = _candleColor };
        chandelier.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.05f, chainLength, 0.05f) },
            Position = new Vector3(0f, chainLength / 2f, 0f),
            MaterialOverride = iron,
        });
        foreach ((float radius, float height, int candles) in _rings)
        {
            chandelier.AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = radius - 0.05f, OuterRadius = radius + 0.05f },
                Position = new Vector3(0f, height, 0f),
                MaterialOverride = iron,
            });
            for (int i = 0; i < candles; i++)
            {
                float angle = Mathf.Tau * i / candles;
                var foot = new Vector3(Mathf.Cos(angle) * radius, height + (CandleHeight / 2f), Mathf.Sin(angle) * radius);
                AddCandle(chandelier, foot, candle, flame);
            }
        }

        chandelier.AddChild(new OmniLight3D
        {
            Position = new Vector3(0f, -0.2f, 0f),
            OmniRange = LightRange,
            LightEnergy = LightEnergy,
            LightColor = _lightColor,
        });
        return chandelier;
    }

    private static void AddCandle(Node3D chandelier, Vector3 centre, Material candle, Material flame)
    {
        chandelier.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.04f, Height = CandleHeight },
            Position = centre,
            MaterialOverride = candle,
        });
        chandelier.AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = _flameSize },
            Position = centre + new Vector3(0f, (CandleHeight + _flameSize.Y) / 2f, 0f),
            MaterialOverride = flame,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }
}
