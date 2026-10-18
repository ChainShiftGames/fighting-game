using Godot;

namespace FightingGame.Presentation.World.Stages.CastleHall;

/// <summary>
/// One long side of the castle hall: the arcade wall with its gallery, half-columns on the piers, banners and torches
/// between them, and the lit aisle behind the arches.
/// </summary>
public static class ArcadeSide
{
    // Developer settings (presentation), in metres; they match the arcade wall shader's layout uniforms.
    private const float BayWidth = 3f;
    private const float PierHeight = 3.25f;
    private const float ColumnRadius = 0.28f;
    private const float BannerHeight = 4.6f;
    private const float TorchHeight = 1.9f;
    private static readonly Vector2 _bannerSize = new(0.9f, 2.3f);
    private static readonly Color[] _bannerColors = [new(0.55f, 0.12f, 0.12f), new(0.13f, 0.2f, 0.45f)];
    private static readonly Color _aisleCeiling = new(0.22f, 0.15f, 0.1f);

    /// <param name="side">-1 for the left side, 1 for the right.</param>
    /// <param name="wallX">How far from the middle the arcade wall stands.</param>
    /// <param name="aisleX">How far from the middle the aisle's outer wall stands.</param>
    /// <param name="front">The hall's near end (towards the camera), in z.</param>
    /// <param name="back">The hall's far end, in z.</param>
    /// <param name="height">The walls' height.</param>
    public static Node3D Build(int side, float wallX, float aisleX, float front, float back, float height)
    {
        var arcade = new Node3D();
        float length = front - back;
        float middle = (front + back) / 2f;
        // A quad faces +z; turned a quarter about y, it faces the middle of the hall.
        var facing = new Vector3(0f, side < 0 ? 90f : -90f, 0f);
        AddWall(arcade, new Vector2(length, height), new Vector3(side * wallX, height / 2f, middle), facing, HallMaterial.Of(
            "arcade_wall", ("wall_size", new Vector2(length, height)), ("bay_width", BayWidth)));
        AddWall(arcade, new Vector2(length, height), new Vector3(side * aisleX, height / 2f, middle), facing, HallMaterial.Of("castle_stone"));
        arcade.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(aisleX - wallX, 0.3f, length) },
            Position = new Vector3(side * (wallX + aisleX) / 2f, PierHeight + 0.15f, middle),
            MaterialOverride = HallMaterial.Of("castle_stone", ("stone_color", _aisleCeiling), ("block_size", new Vector2(3f, 0.25f))),
        });

        int piers = (int)(length / BayWidth);
        for (int pier = 0; pier <= piers; pier++)
        {
            float z = front - (pier * BayWidth);
            AddColumn(arcade, new Vector3(side * (wallX - ColumnRadius), 0f, z));
            if (pier % 2 == 0)
            {
                AddBanner(arcade, new Vector3(side * (wallX - 0.06f), BannerHeight, z), facing, _bannerColors[pier / 2 % 2]);
            }
            else
            {
                arcade.AddChild(WallTorch.At(new Vector3(side * (wallX - (2f * ColumnRadius)), TorchHeight, z)));
            }

            if (pier < piers && pier % 2 == 0)
            {
                arcade.AddChild(WallTorch.At(new Vector3(side * (aisleX - 0.15f), TorchHeight, z - (BayWidth / 2f))));
            }
        }

        return arcade;
    }

    private static void AddWall(Node3D arcade, Vector2 size, Vector3 centre, Vector3 rotation, Material material) =>
        arcade.AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = size },
            Position = centre,
            RotationDegrees = rotation,
            MaterialOverride = material,
        });

    private static void AddColumn(Node3D arcade, Vector3 foot)
    {
        ShaderMaterial stone = HallMaterial.Of("castle_stone", ("stone_color", new Color(0.8f, 0.71f, 0.57f)));
        arcade.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = ColumnRadius, BottomRadius = ColumnRadius, Height = PierHeight },
            Position = foot + new Vector3(0f, PierHeight / 2f, 0f),
            MaterialOverride = stone,
        });
        foreach (float y in new[] { 0.15f, PierHeight - 0.15f })
        {
            arcade.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.8f, 0.3f, 0.8f) },
                Position = foot + new Vector3(0f, y, 0f),
                MaterialOverride = stone,
            });
        }
    }

    private static void AddBanner(Node3D arcade, Vector3 centre, Vector3 rotation, Color field) =>
        arcade.AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = _bannerSize },
            Position = centre,
            RotationDegrees = rotation,
            MaterialOverride = HallMaterial.Of("banner", ("field_color", field)),
        });
}
