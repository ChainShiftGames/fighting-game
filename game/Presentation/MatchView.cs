using System.Collections.Generic;
using FightingGame.Debug;
using FightingGame.Presentation.Events;
using FightingGame.Presentation.World;
using FightingGame.Presentation.World.Projectiles;
using FightingGame.Presentation.World.Sparks;
using Godot;
using Netcode.Drivers;
using Sim.Characters;
using Sim.Collision;
using Sim.Math;
using Sim.Rules;
using Sim.State;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Presentation;

/// <summary>
/// Shows a match of the real game logic from the driver's state: the 3D stage and fighters on the gameplay plane
/// (ADR-0027) with the hit sparks over them, then the hitbox overlay when it is on, fighter readouts and the HUD, all
/// shaken by the screen shake. The camera rises and zooms out to keep high fighters in view
/// (<see cref="VerticalFraming"/>). Reads the state only (ARCHITECTURE.md §9).
/// </summary>
public partial class MatchView : Node2D
{
    // Blue and orange: told apart with the common kinds of colour blindness (ADR-0008 §5).
    private static readonly Color[] _fighterColors = [new(0.25f, 0.55f, 1f), new(1f, 0.6f, 0.15f)];

    private readonly List<WorldBox> _boxes = [];
    private readonly MatchHud _hud = new();
    private readonly MatchCamera _worldCamera = new();
    private readonly VerticalFollow _heightFollow = new();
    private readonly StageSet _stage = new();
    private readonly ProjectileViews _projectiles = new();
    private readonly ChargeViews _charges = new();
    private readonly ImpactViews _impacts = new();
    private readonly HitSparkViews _sparks = new();
    private HitSparkLooks? _sparkLooks;
    private readonly Node3D?[] _fighterNodes = new Node3D?[MatchState.PlayerCount];
    private readonly IFighterArt?[] _fighterArt = new IFighterArt?[MatchState.PlayerCount];
    private IMatchDriver<MatchState>? _driver;
    private CharacterData[] _characters = [];
    private LogicScreen _screen;
    private DeveloperToggle? _hitboxOverlay;
    private MatchEffects? _effects;
    private StageCamera? _camera;
    private System.Func<Sim.Events.FrameEvent, bool>? _sparkDrawnElsewhere;
    private readonly BackdropBlur _backdropBlur = new();
    private readonly InkLines _inkLines = new();
    private readonly FighterBrush _brush = new();
    private LookToggles? _look;
    private int _modelVariant;
    private AnimeGrade? _grade;

    /// <param name="characters">P1's and P2's character, for their boxes and move names.</param>
    /// <param name="rules">The match rules, for the HUD's round count and gauges.</param>
    /// <param name="screen">The logic camera's width and the stage walls, from the match rules.</param>
    /// <param name="hitboxOverlay">Draws every box when on (developer setting).</param>
    /// <param name="effects">The match's event effects; its sounds join the scene as a child of this view.</param>
    public void Show(
        IMatchDriver<MatchState> driver,
        CharacterData[] characters,
        in MatchRules rules,
        LogicScreen screen,
        DeveloperToggle hitboxOverlay,
        MatchEffects effects)
    {
        _driver = driver;
        _characters = characters;
        _screen = screen;
        _hitboxOverlay = hitboxOverlay;
        _effects = effects;
        AddChild(effects.Sounds);
        _hud.Show(driver, characters, in rules);
        _stage.Build(screen.Stage);
        _worldCamera.UsePerspective = _stage.WantsPerspective;
        ProjectileArtSettings?[][] projectileArt = ProjectileArtLookup.Of(characters);
        _projectiles.Build(characters, projectileArt);
        _charges.Build(characters, projectileArt);
        _impacts.Build(projectileArt);
        _sparkDrawnElsewhere = _impacts.HasArt;
        _sparkLooks = new HitSparkLooks(
            characters,
            System.Array.ConvertAll(characters, c => FighterArtChoice.ReadSettings(c.Id)?.Sparks),
            effects.Settings.Sparks);
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            CreateFighter(player);
        }
    }

    /// <summary>
    /// Redraws both fighters with model <paramref name="variant"/> of their art files (developer setting, see
    /// <see cref="FighterArtSettings.WithModel"/>), in place, so models can be compared mid-match.
    /// </summary>
    /// <returns>P1's model file now shown; empty for a placeholder.</returns>
    public string ShowModel(int variant)
    {
        if (variant != _modelVariant)
        {
            SwapModels(variant);
        }

        return _characters.Length == 0
            ? string.Empty
            : FighterArtChoice.ReadSettings(_characters[0].Id)?.WithModel(variant).Model ?? string.Empty;
    }

    private void SwapModels(int variant)
    {
        _modelVariant = variant;
        for (int player = 0; player < MatchState.PlayerCount && _characters.Length > 0; player++)
        {
            Node3D? old = _fighterNodes[player];
            CreateFighter(player);
            if (old is null)
            {
                continue;
            }

            if (!old.IsInsideTree())
            {
                // Not shown yet: _Ready adds the new one.
                old.Free();
                continue;
            }

            int index = old.GetIndex();
            old.QueueFree();
            AddChild(_fighterNodes[player]);
            MoveChild(_fighterNodes[player]!, index);
        }
    }

    private void CreateFighter(int player) =>
        _fighterNodes[player] = FighterArtChoice.Create(
            _characters[player], _fighterColors[player], _modelVariant, out _fighterArt[player]);

    /// <summary>The whole-screen looks to follow (developer settings); without them the stage shows its own look.</summary>
    public void FollowLook(LookToggles look) => _look = look;

    public override void _Ready()
    {
        AddChild(_stage);
        _worldCamera.Attributes = _backdropBlur.Attributes;
        AddChild(_worldCamera);
        _worldCamera.AddChild(_inkLines.Pass);
        foreach (Node3D? fighter in _fighterNodes)
        {
            if (fighter is not null)
            {
                AddChild(fighter);
            }
        }

        AddChild(_charges);
        AddChild(_projectiles);
        AddChild(_impacts);
        AddChild(_sparks);
        AddChild(_hud);
    }

    /// <summary>Brings the 3D scene and the 2D layers up to the driver's state; call once per displayed frame.</summary>
    public void Refresh()
    {
        if (_driver is null || !IsInsideTree())
        {
            return;
        }

        ref readonly MatchState state = ref _driver.State;
        Vector2 size = GetViewportRect().Size;
        StageCamera fitted = StageCamera.Fit(size, _screen, state.Camera.X, FollowFighters(in state, size));
        StageCamera camera = fitted with { FloorCentre = fitted.FloorCentre + _effects!.Shake.Offset(state.Frame - 1) };
        _camera = camera;
        CameraFraming framing = CameraFraming.Of(camera, size);
        _worldCamera.Frame(framing);
        _stage.FollowCamera(framing.Centre.X);
        ShowLook();
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            ref readonly FighterState fighter = ref state.Fighters[player];
            CollectBoxes(in fighter, _characters[player]);
            FighterPose pose = FighterPose.Of(_characters[player], in fighter) with { MatchFrame = state.Frame };
            IntVector2 drawnAt = fighter.Position + _effects.DefenderShake.Offset(in pose);
            _fighterArt[player]!.ShowPose(pose, drawnAt, fighter.Facing, _boxes);
        }

        _charges.ShowFrame(in state.Fighters);
        _projectiles.ShowFrame(in state.Entities);
        int latestFrame = state.Frame - 1;
        _impacts.ShowFrame(_effects.Impacts.Live(latestFrame), latestFrame, in state.Fighters);
        _sparks.ShowFrame(_effects.Sparks.Live(latestFrame, in state.Fighters), _sparkLooks!, _sparkDrawnElsewhere);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_driver is null || _camera is not { } camera)
        {
            return;
        }

        ref readonly MatchState state = ref _driver.State;
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            bool drawLimbs = !_fighterArt[player]!.HasLimbs;
            DrawReadouts(camera, in state.Fighters[player], _characters[player], _fighterColors[player], drawLimbs);
        }
    }

    // The stage brings its own environment; the grade is laid over it once the stage is in the scene.
    private void ShowLook()
    {
        _grade ??= SceneSearch.First<WorldEnvironment>(_stage)?.Environment is { } environment
            ? new AnimeGrade(environment)
            : null;
        _grade?.Show(_look?.Grade.Enabled ?? false);
        bool blurred = (_look?.BackdropBlur.Enabled ?? false) && _worldCamera.Projection == Camera3D.ProjectionType.Perspective;
        _backdropBlur.Show(blurred, _worldCamera.PlaneDistance);
        _brush.Show(_look?.BrushShading.Enabled ?? true);
        _inkLines.Show(_look?.InkLines.Enabled ?? true, _worldCamera.PlaneDistance);
    }

    // The 2D layers and the 3D camera both take their framing from this one view, so they stay lined up (ADR-0027 §3).
    private VerticalSpan FollowFighters(in MatchState state, Vector2 size)
    {
        System.Span<VerticalSpan> bodies = stackalloc VerticalSpan[MatchState.PlayerCount];
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            ref readonly FighterState fighter = ref state.Fighters[player];
            CollectBoxes(in fighter, _characters[player]);
            bodies[player] = FighterBoxes.Height(fighter.Position, _boxes);
        }

        return _heightFollow.Follow(state.Frame, StageCamera.GroundView(size, _screen), bodies);
    }

    private void CollectBoxes(in FighterState fighter, CharacterData character)
    {
        _boxes.Clear();
        FighterBoxes.Collect(character, in fighter, _boxes);
    }

    private void DrawReadouts(
        StageCamera camera, in FighterState fighter, CharacterData character, Color color, bool drawLimbs)
    {
        CollectBoxes(in fighter, character);
        FighterReadoutArt.Draw(this, camera, FighterPose.Of(character, in fighter), _boxes, color, drawLimbs);
        if (_hitboxOverlay!.Enabled)
        {
            BoxOverlayArt.Draw(this, camera, _boxes);
        }
    }
}
