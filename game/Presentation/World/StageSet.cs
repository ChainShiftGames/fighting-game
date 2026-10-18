using Godot;
using Sim.Collision;

namespace FightingGame.Presentation.World;

/// <summary>
/// The placeholder stage in the 3D scene: a floor slab between the walls with its top on the logic floor, solid walls
/// standing just outside the logic walls, a fixed light and a plain background, or the picture the stage art file
/// names standing behind the plane, its ground on the logic floor and in place of the slab.
/// </summary>
/// <remarks>Stage art replaces this; it keeps the walls at the logic's positions (P4-D).</remarks>
public partial class StageSet : Node3D
{
    // Developer settings (presentation), in metres: how far the floor reaches down and back, the walls' size, and the
    // light's angle (down and from the front left).
    private const float FloorThickness = 3f;
    private const float StageDepth = 6f;
    private const float WallThickness = 2f;
    private const float WallHeight = 40f;
    // How far the background picture reaches beyond each wall, so the view never shows its edge.
    private const float BackgroundBeyondWalls = 3f;
    private static readonly Vector3 _lightAngles = new(-50f, -30f, 0f);

    private static readonly Color _floorColor = new(0.35f, 0.35f, 0.4f);
    private static readonly Color _wallColor = new(0.2f, 0.2f, 0.24f);
    private static readonly Color _backgroundColor = new(0.08f, 0.08f, 0.1f);
    private static readonly Color _ambientColor = new(0.5f, 0.5f, 0.55f);

    private StageWalls _walls;
    private MeshInstance3D? _background;
    private float _parallax;

    /// <summary>Builds the stage for <paramref name="walls"/>; call before the set joins the scene.</summary>
    public void Build(StageWalls walls) => _walls = walls;

    public override void _Ready()
    {
        float left = WorldScale.ToMetres(_walls.Left);
        float right = WorldScale.ToMetres(_walls.Right);
        if (!AddBackground(left, right))
        {
            AddBlock(new Vector3(right - left, FloorThickness, StageDepth), new Vector3((left + right) / 2f, -FloorThickness / 2f, 0f), _floorColor);
        }

        AddBlock(new Vector3(WallThickness, WallHeight, StageDepth), new Vector3(left - (WallThickness / 2f), WallHeight / 2f, 0f), _wallColor);
        AddBlock(new Vector3(WallThickness, WallHeight, StageDepth), new Vector3(right + (WallThickness / 2f), WallHeight / 2f, 0f), _wallColor);
        AddChild(new DirectionalLight3D { RotationDegrees = _lightAngles });
        AddChild(new WorldEnvironment { Environment = Background() });
    }

    /// <summary>Moves the background picture by its parallax share of the camera's position; once per frame.</summary>
    /// <param name="cameraX">The gameplay plane point at the middle of the screen, in metres.</param>
    public void FollowCamera(float cameraX)
    {
        if (_background is not null)
        {
            _background.Position = _background.Position with { X = cameraX * _parallax };
        }
    }

    private bool AddBackground(float left, float right)
    {
        if (ReadArt() is not { Background.Length: > 0 } art)
        {
            return false;
        }

        string path = StageArtFile.Resolve(art.Background);
        if (!ResourceLoader.Exists(path) || ResourceLoader.Load(path) is not Texture2D picture)
        {
            GD.PushError($"{path}: no imported picture there; showing the plain stage");
            return false;
        }

        // Following the camera by the parallax share, the picture only has to span the rest of the camera's travel.
        float width = ((right - left) * (1f - art.Parallax)) + (2f * BackgroundBeyondWalls);
        float height = width * picture.GetHeight() / picture.GetWidth();
        _parallax = art.Parallax;
        _background = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(width, height) },
            Position = new Vector3((left + right) / 2f, (height / 2f) - (art.GroundLine * height), -art.Depth),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoTexture = picture,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                // Pixel art stays crisp when the camera scales it.
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            },
        };
        AddChild(_background);
        return true;
    }

    private static StageArtSettings? ReadArt()
    {
        if (!FileAccess.FileExists(StageArtFile.Path))
        {
            return null;
        }

        StageArtSettings? art = StageArtFile.Read(FileAccess.GetFileAsString(StageArtFile.Path), out string error);
        if (art is null)
        {
            GD.PushError($"{StageArtFile.Path}: {error}; showing the plain stage");
        }

        return art;
    }

    private void AddBlock(Vector3 size, Vector3 centre, Color color) => AddChild(new MeshInstance3D
    {
        Mesh = new BoxMesh { Size = size },
        Position = centre,
        MaterialOverride = new StandardMaterial3D { AlbedoColor = color },
    });

    private static Godot.Environment Background() => new()
    {
        BackgroundMode = Godot.Environment.BGMode.Color,
        BackgroundColor = _backgroundColor,
        AmbientLightSource = Godot.Environment.AmbientSource.Color,
        AmbientLightColor = _ambientColor,
    };
}
