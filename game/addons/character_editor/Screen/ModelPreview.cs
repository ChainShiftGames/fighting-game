#if TOOLS
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Presentation;
using FightingGame.Presentation.World;
using Godot;
using Sim.Math;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The character's rigged model, posed for the frame on the <see cref="BoxCanvas"/> and drawn behind its boxes, so
/// boxes can be fitted to the body (ADR-0028). Renders off-screen, framed to the canvas's own origin and zoom; a
/// character drawn as a placeholder has no model and the canvas shows its body box instead. The move's projectile
/// effects (charge, then flight) are drawn with it, where the match draws them.
/// </summary>
[Tool]
public partial class ModelPreview : SubViewport
{
    // Developer settings (presentation, editor): the camera's distance and depth, and the light as on the stage.
    private const float CameraDistance = 20f;
    private const float ViewDepth = 200f;
    private static readonly Vector3 _lightAngles = new(-50f, -30f, 0f);
    private static readonly Color _ambientColor = new(0.5f, 0.5f, 0.55f);

    private static readonly List<WorldBox> _noBoxes = [];

    private readonly Camera3D _camera = new()
    {
        Projection = Camera3D.ProjectionType.Orthogonal,
        KeepAspect = Camera3D.KeepAspectEnum.Height,
        Far = ViewDepth,
        Current = true,
    };

    private readonly PreviewProjectiles _projectiles = new();
    private RiggedFighterModel? _model;

    public ModelPreview()
    {
        OwnWorld3D = true;
        TransparentBg = true;
        RenderTargetUpdateMode = UpdateMode.Always;
        AddChild(_camera);
        AddChild(_projectiles);
        AddChild(new DirectionalLight3D { RotationDegrees = _lightAngles });
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = _ambientColor,
            },
        });
    }

    public bool HasModel => _model is not null;

    /// <summary>How the model retimes a move's clip now; null without a model or a clip for the move.</summary>
    public AnimationClipTiming? TimingOf(string moveId) => _model?.TimingOf(moveId);

    /// <summary>Retimes the move's clip as the animation track edits it, until the character is shown again.</summary>
    /// <returns>Why some of the timing was left out; empty when all of it applies.</returns>
    public string Retime(string moveId, ContactFrames? contacts, IReadOnlyList<TimingKey> keys) =>
        _model?.Retime(moveId, contacts, keys) ?? string.Empty;

    /// <summary>
    /// Loads the character's model from its art file, again on each call so Reload picks up art changes; null or a
    /// placeholder character clears it.
    /// </summary>
    public void ShowCharacter(string? characterId, IReadOnlyList<string> moveIds)
    {
        _model?.QueueFree();
        _model = characterId is null ? null : FighterArtChoice.CreateModel(characterId, moveIds);
        _projectiles.ShowCharacter(
            characterId is not null && _model is not null && FighterArtChoice.ReadSettings(characterId) is { } art
                ? art.Projectiles
                : new Dictionary<string, Presentation.World.Projectiles.ProjectileArtSettings>());
        if (_model is not null)
        {
            AddChild(_model);
        }
    }

    /// <summary>Poses the model for <paramref name="frame"/> of <paramref name="move"/>, facing right at the origin.</summary>
    /// <param name="floorCentre">Where the canvas draws the data origin, in canvas pixels.</param>
    /// <param name="pixelsPerSubpixel">The canvas's zoom.</param>
    public void ShowFrame(JsonObject move, int frame, Vector2 canvasSize, Vector2 floorCentre, float pixelsPerSubpixel)
    {
        Size = new Vector2I(Mathf.Max(1, (int)canvasSize.X), Mathf.Max(1, (int)canvasSize.Y));
        CameraFraming framing = CameraFraming.Of(new StageCamera(floorCentre, pixelsPerSubpixel), canvasSize);
        _camera.Size = framing.VisibleHeight;
        _camera.Position = framing.Centre + new Vector3(0f, 0f, CameraDistance);
        if (_model is not null && MovePose.Of(move, frame) is { } pose)
        {
            _model.ShowPose(pose, new IntVector2(0, 0), 1, _noBoxes);
            _projectiles.ShowFrame(move, frame);
        }
    }
}
#endif
