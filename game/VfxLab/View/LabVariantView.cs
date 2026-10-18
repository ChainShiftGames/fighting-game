using System.Collections.Generic;
using FightingGame.Presentation.World;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab.View;

/// <summary>
/// One variant playing in its own 3D world: the set, a card per part and the match camera, rendered into this
/// container. Every view of the grid shows the same frame.
/// </summary>
public partial class LabVariantView : SubViewportContainer
{
    private readonly SubViewport _viewport = new() { OwnWorld3D = true, HandleInputLocally = false };
    private readonly LabSet _set = new();
    private readonly MatchCamera _camera = new();
    private readonly List<LabEffectCard> _cards = [];

    /// <summary>The parts whose shader was missing or failed to load; empty when all loaded.</summary>
    public List<string> Missing { get; } = [];

    public IReadOnlyList<LabEffectCard> Cards => _cards;

    /// <summary>Makes a card per part of <paramref name="variant"/>; call once, before the view joins the scene.</summary>
    public void Build(LabVariant variant, IReadOnlyDictionary<string, float[]> ranges, LabStagePicture? picture)
    {
        Stretch = true;
        MouseFilter = MouseFilterEnum.Pass;
        AddChild(_viewport);
        _viewport.AddChild(_set);
        _viewport.AddChild(_camera);
        if (picture is not null)
        {
            _set.UsePicture(picture.Texture, picture.Rect);
        }

        foreach ((LabPartKind kind, LabPart part) in variant.Parts)
        {
            if (!ResourceLoader.Exists(part.Shader) || ResourceLoader.Load(part.Shader) is not Shader shader)
            {
                Missing.Add($"{kind.ToString().ToLowerInvariant()}: {part.Shader}");
                continue;
            }

            LabEffectCard card = new();
            card.Build(kind, part, shader, ranges);
            _cards.Add(card);
            _viewport.AddChild(card);
        }
    }

    public void Apply(IReadOnlyDictionary<string, LabValue> values)
    {
        foreach (LabEffectCard card in _cards)
        {
            card.Apply(values);
        }
    }

    public void ShowFrame(LabMoment moment, LabTiming timing, LabViewOptions options)
    {
        int facing = options.Mirror ? -1 : 1;
        (float x, float y) = LabLayout.EffectPoint(timing, moment);
        Vector3 point = new(facing * x / LabSet.PixelsPerMetre, y / LabSet.PixelsPerMetre, 0f);
        float spawnSize = SpawnSize();
        foreach (LabEffectCard card in _cards)
        {
            card.ShowFrame(moment, timing, options.Impact, facing, point, spawnSize);
        }

        bool flying = moment.Phase == LabPhase.Flight;
        _set.ShowFrame(timing, options, facing, flying ? point : null);

        float aspect = Size.Y > 0f ? Size.X / Size.Y : 16f / 9f;
        LabShot shot = LabShot.Of(timing, moment, options.Framing, options.Zoom, aspect);
        _camera.Frame(new CameraFraming(
            new Vector3(facing * shot.CentreX / LabSet.PixelsPerMetre, shot.CentreY / LabSet.PixelsPerMetre, 0f),
            shot.VisibleHeight / LabSet.PixelsPerMetre));
    }

    private float SpawnSize()
    {
        foreach (LabEffectCard card in _cards)
        {
            if (card.Kind == LabPartKind.Flight)
            {
                return card.Setting(LabPartSettings.StartScale) * card.Setting(LabPartSettings.Scale);
            }
        }

        return LabPartDraft.DefaultStartScale;
    }
}
