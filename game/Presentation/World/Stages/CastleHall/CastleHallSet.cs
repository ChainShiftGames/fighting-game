using Godot;

namespace FightingGame.Presentation.World.Stages.CastleHall;

/// <summary>
/// The castle hall stage, built from shaders and simple meshes: a checkered floor running down a long hall between two
/// arcades with galleries, stained-glass windows at the far end, chandeliers and bunting overhead, torches and banners on
/// the piers. The gameplay plane runs across the hall near its front; the arcade walls stand just outside the logic walls.
/// </summary>
public partial class CastleHallSet : Node3D
{
    // Developer settings (presentation), in metres. The arcade walls stand just outside the logic walls (±8.57 m).
    private const float WallX = 9.2f;
    private const float AisleX = 12.5f;
    private const float Front = 6f;
    private const float Back = -18f;
    private const float Height = 7.5f;
    // Overhead decoration sits where the tight perspective camera still shows it: higher the further back it hangs.
    // Bunting rows are (z, height); chandeliers are where their lowest ring hangs.
    private static readonly Vector2[] _buntingRows = [new(-15f, 5.1f)];
    private static readonly Vector3[] _chandeliers = [new(-3f, 3.7f, -11f), new(3f, 3.7f, -11f), new(0f, 4.5f, -16f)];
    private static readonly Vector3 _keyLightAngles = new(-35f, -20f, 0f);
    private static readonly Color _keyLightColor = new(1f, 0.9f, 0.78f);
    private static readonly Color _ceilingColor = new(0.2f, 0.13f, 0.09f);
    private static readonly Color _ambientColor = new(0.6f, 0.5f, 0.42f);
    private static readonly Color _fogColor = new(0.62f, 0.48f, 0.34f);
    private static readonly Color _backgroundColor = new(0.05f, 0.04f, 0.04f);

    public override void _Ready()
    {
        float middle = (Front + Back) / 2f;
        AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2((2f * AisleX) + 2f, Front - Back + 4f) },
            Position = new Vector3(0f, 0f, middle),
            MaterialOverride = HallMaterial.Of("checker_floor"),
        });
        AddChild(ArcadeSide.Build(-1, WallX, AisleX, Front, Back, Height));
        AddChild(ArcadeSide.Build(1, WallX, AisleX, Front, Back, Height));
        var endWall = new Vector2(2f * AisleX, Height);
        AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = endWall },
            Position = new Vector3(0f, Height / 2f, Back),
            MaterialOverride = HallMaterial.Of("end_wall", ("wall_size", endWall)),
        });
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(2f * AisleX, 0.4f, Front - Back) },
            Position = new Vector3(0f, Height + 0.2f, middle),
            MaterialOverride = HallMaterial.Of("castle_stone", ("stone_color", _ceilingColor), ("block_size", new Vector2(3f, 0.3f))),
        });
        AddDecorations();
        AddLighting();
    }

    private void AddDecorations()
    {
        ShaderMaterial bunting = HallMaterial.Of("bunting");
        foreach (Vector2 row in _buntingRows)
        {
            AddChild(new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(2f * WallX, 0.6f), SubdivideWidth = 32 },
                Position = new Vector3(0f, row.Y, row.X),
                MaterialOverride = bunting,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }

        foreach (Vector3 hook in _chandeliers)
        {
            AddChild(Chandelier.At(hook, Height - hook.Y));
        }
    }

    private void AddLighting()
    {
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = _keyLightAngles,
            LightColor = _keyLightColor,
            LightEnergy = 1.2f,
            ShadowEnabled = true,
        });
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = _backgroundColor,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = _ambientColor,
                AmbientLightEnergy = 1f,
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
                GlowEnabled = true,
                GlowIntensity = 0.8f,
                GlowHdrThreshold = 1f,
                FogEnabled = true,
                FogLightColor = _fogColor,
                FogDensity = 0.016f,
            },
        });
    }
}
