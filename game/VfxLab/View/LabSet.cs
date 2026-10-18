using FightingGame.Presentation.World;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab.View;

/// <summary>
/// What a Lab view stands the effect in: a backdrop, stand-ins for the caster and the target (their body boxes, as
/// the placeholder fighters are drawn) and the projectile's hitbox outline.
/// </summary>
public partial class LabSet : Node3D
{
    /// <summary>World pixels per metre (ADR-0027: 1 m = 70 px).</summary>
    public const float PixelsPerMetre = WorldScale.SubpixelsPerMetre / 1000f;

    // Developer settings (presentation): the backdrop's size and distance behind the plane, in metres, and the
    // stand-ins' depth (as the placeholder fighter's) and colours.
    private const float BackdropWidth = 80f;
    private const float BackdropHeight = 40f;
    private const float BackdropDepth = 3f;
    private const float StandInDepth = 0.4f;
    private const float OutlineDepth = 0.3f;
    private static readonly Color _casterColor = new(0.16f, 0.15f, 0.2f);
    private static readonly Color _targetColor = new(0.24f, 0.2f, 0.2f);

    private static readonly StringName _mode = "mode";
    private static readonly StringName _dim = "dim";
    private static readonly StringName _picture = "picture";
    private static readonly StringName _pictureRect = "picture_rect";
    private static readonly StringName _boxSize = "box_size";

    private readonly ShaderMaterial _backdrop = new() { Shader = GD.Load<Shader>("res://VfxLab/View/lab_backdrop.gdshader") };
    private readonly ShaderMaterial _outline = new() { Shader = GD.Load<Shader>("res://VfxLab/View/lab_outline.gdshader") };
    private readonly MeshInstance3D _caster = StandIn(_casterColor);
    private readonly MeshInstance3D _target = StandIn(_targetColor);
    private readonly MeshInstance3D _hitbox = new() { Mesh = new QuadMesh { Size = Vector2.One } };

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(BackdropWidth, BackdropHeight) },
            Position = new Vector3(0f, (BackdropHeight / 2f) - 4f, -BackdropDepth),
            MaterialOverride = _backdrop,
        });
        _hitbox.MaterialOverride = _outline;
        AddChild(_caster);
        AddChild(_target);
        AddChild(_hitbox);
    }

    /// <summary>Shows the stage art file's picture for <see cref="LabBackdrop.StagePicture"/>.</summary>
    /// <param name="rect">Where it stands in metres: left, bottom, width, height.</param>
    public void UsePicture(Texture2D picture, Vector4 rect)
    {
        _backdrop.SetShaderParameter(_picture, picture);
        _backdrop.SetShaderParameter(_pictureRect, rect);
    }

    /// <param name="facing">1 when the caster faces right; -1 mirrors the whole set.</param>
    /// <param name="point">The effect's point, in metres, for the hitbox outline; null hides it.</param>
    public void ShowFrame(LabTiming timing, LabViewOptions options, int facing, Vector3? point)
    {
        _backdrop.SetShaderParameter(_mode, (int)options.Backdrop);
        _backdrop.SetShaderParameter(_dim, options.SuperFlash ? 1f : 0f);

        Vector3 body = new(timing.FighterWidth / PixelsPerMetre, timing.FighterHeight / PixelsPerMetre, StandInDepth);
        _caster.Visible = _target.Visible = options.StandIns;
        _caster.Scale = _target.Scale = body;
        _caster.Position = new Vector3(0f, body.Y / 2f, 0f);
        _target.Position = new Vector3(facing * LabLayout.TargetX(timing) / PixelsPerMetre, body.Y / 2f, 0f);

        _hitbox.Visible = options.Hitbox && point is not null;
        if (point is { } at)
        {
            float size = timing.HitboxSize / PixelsPerMetre;
            _hitbox.Scale = new Vector3(size, size, 1f);
            _hitbox.Position = at with { Z = OutlineDepth };
            _outline.SetShaderParameter(_boxSize, new Vector2(size, size));
        }
    }

    private static MeshInstance3D StandIn(Color color) => new()
    {
        Mesh = new BoxMesh { Size = Vector3.One },
        MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = color,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        },
    };
}
