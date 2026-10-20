using System;
using System.Collections.Generic;
using FightingGame.Presentation.World;
using FightingGame.Presentation.World.Projectiles;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab.View;

/// <summary>
/// One part of a variant on its card, driven the way the game's art nodes drive theirs (ADR-0029 §5): the clock from
/// the part's own frames, and <c>progress</c>, <c>size</c> or <c>age</c> for its part.
/// </summary>
public partial class LabEffectCard : Node3D
{
    private static readonly StringName _size = "size";
    private static readonly StringName _progress = "progress";
    private static readonly StringName _spawnSize = "spawn_size";
    private static readonly StringName _age = "age";
    private static readonly StringName _kind = "kind";
    private static readonly StringName _lifetime = "lifetime";
    private static readonly StringName _cardCentre = "card_centre";

    // Developer settings (presentation): a part's model sits this far before its card, and a charge's model forms
    // over the startup after this share of it, glowing as it does; as the ice orb's art nodes.
    private const float ModelInFront = 0.12f;
    private const float ModelFormsFrom = 0.6f;
    private const float FormingFlare = 0.8f;

    private readonly ShaderCard _card = new();
    private ProjectileModel? _model;
    private readonly Dictionary<string, UniformSpec> _uniforms = new(StringComparer.Ordinal);
    private Shader _shader = null!;
    private LabPart _part = null!;
    private IReadOnlyDictionary<string, LabValue> _values = new Dictionary<string, LabValue>();
    private Vector2 _cardSize;

    public LabPartKind Kind { get; private set; }

    public LabPart Part => _part;

    /// <summary>The uniforms that get a control, in the shader's order.</summary>
    public List<UniformSpec> Uniforms { get; } = [];

    /// <summary>Call once, before the card joins the scene.</summary>
    public void Build(LabPartKind kind, LabPart part, Shader shader, IReadOnlyDictionary<string, float[]> ranges)
    {
        Kind = kind;
        _part = part;
        _shader = shader;
        Uniforms.AddRange(ShaderUniforms.Of(shader, ranges));
        foreach (UniformSpec spec in Uniforms)
        {
            _uniforms[spec.Name] = spec;
        }

        AddChild(_card);
        if (part.Model is { } model && ResourceLoader.Exists(model))
        {
            _model = new ProjectileModel { Position = new Vector3(0f, 0f, kind == LabPartKind.Flight ? ModelInFront : 0f) };
            _model.Build(model);
            AddChild(_model);
        }

        Resize();
    }

    /// <summary>Sets every uniform this part's shader has from <paramref name="values"/>; the rest keep their defaults.</summary>
    public void Apply(IReadOnlyDictionary<string, LabValue> values)
    {
        _values = values;
        foreach (UniformSpec spec in Uniforms)
        {
            LabValue value = values.TryGetValue(spec.Name, out LabValue set) ? set : spec.Default;
            _card.SetParameter(spec.Name, LabValueVariants.ToVariant(value, spec));
        }

        Resize();
    }

    /// <param name="point">The effect's point on the gameplay plane, in metres.</param>
    /// <param name="spawnSize">The flight's size on its first frame, which the charge's seed grows to.</param>
    public void ShowFrame(LabMoment moment, LabTiming timing, ImpactKind impact, int facing, Vector3 point, float spawnSize)
    {
        Visible = moment.Phase == PhaseOf(Kind);
        if (!Visible)
        {
            return;
        }

        float scale = Setting(LabPartSettings.Scale);
        Position = point;
        Scale = new Vector3(facing * scale, scale, scale);
        float spin = Setting(LabPartSettings.Spin);
        switch (Kind)
        {
            case LabPartKind.Charge:
                float progress = DarkFireballChargeArt.ProgressAt(moment.PhaseFrame, timing.Charge + 1);
                _card.SetClock(moment.PhaseFrame, spin);
                _card.SetParameter(_progress, progress);
                _card.SetParameter(_spawnSize, spawnSize);
                float forming = Mathf.Clamp((progress - ModelFormsFrom) / (1f - ModelFormsFrom), 0f, 1f);
                ShowModel(moment.PhaseFrame, _part.ModelSize * spawnSize * forming, FormingFlare * (1f - forming));
                break;
            case LabPartKind.Flight:
                float size = SizeAt(moment.PhaseFrame);
                _card.SetClock(moment.PhaseFrame, spin);
                _card.SetParameter(_size, size);
                ShowModel(moment.PhaseFrame, _part.ModelSize * size, 0f);
                break;
            default:
                _card.SetClock(moment.PhaseFrame, spin);
                _card.SetParameter(_age, (float)(moment.PhaseFrame - 1));
                _card.SetParameter(_kind, (int)impact);
                _card.SetParameter(_lifetime, (float)timing.Impact);
                break;
        }
    }

    private void ShowModel(int frame, float across, float flare) => _model?.ShowFrame(
        across, frame * _part.ModelRoll, (frame - 1) / AnimationRetiming.FramesPerSecond, flare);

    /// <summary>The value of <paramref name="setting"/> for this part, the devs' changes included.</summary>
    public float Setting(LabPartSetting setting) => LabPartSettings.Read(setting, Kind, _part, _values);

    private static LabPhase PhaseOf(LabPartKind kind) => kind switch
    {
        LabPartKind.Charge => LabPhase.Charge,
        LabPartKind.Flight => LabPhase.Flight,
        _ => LabPhase.Impact,
    };

    // As DarkFireballArt.SizeAt, with the grow settings from the sheet.
    private float SizeAt(int frame)
    {
        float start = Setting(LabPartSettings.StartScale);
        float frames = Setting(LabPartSettings.GrowFrames);
        float grown = frames <= 0f ? 1f : Mathf.Clamp((frame - 1) / frames, 0f, 1f);
        return start + ((1f - start) * (1f - ((1f - grown) * (1f - grown))));
    }

    private void Resize()
    {
        Vector2 size = new(Setting(LabPartSettings.CardWidth), Setting(LabPartSettings.CardHeight));
        if (size != _cardSize)
        {
            _cardSize = size;
            Vector2 centre = new(_part.CentreX, _part.CentreY);
            _card.Build(_shader, size, centre);
            _card.SetParameter(_cardCentre, centre);
        }
    }
}
