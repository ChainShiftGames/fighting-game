using System.Collections.Generic;
using FightingGame.Debug;
using FightingGame.Presentation.Events;
using FightingGame.Presentation.World;
using FightingGame.Presentation.World.Projectiles;
using Godot;
using Netcode.Drivers;
using Sim.Characters;
using Sim.Collision;
using Sim.Math;
using Sim.State;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Presentation;

/// <summary>
/// Shows a match of the real game logic from the driver's state: the 3D stage and fighters on the gameplay plane
/// (ADR-0027), and over them hit sparks, the hitbox overlay when it is on, fighter readouts and the HUD, all shaken by
/// the screen shake. Reads the state only (ARCHITECTURE.md §9).
/// </summary>
public partial class MatchView : Node2D
{
    // Blue and orange: told apart with the common kinds of colour blindness (ADR-0008 §5).
    private static readonly Color[] _fighterColors = [new(0.25f, 0.55f, 1f), new(1f, 0.6f, 0.15f)];

    private readonly List<WorldBox> _boxes = [];
    private readonly MatchHud _hud = new();
    private readonly MatchCamera _worldCamera = new();
    private readonly StageSet _stage = new();
    private readonly ProjectileViews _projectiles = new();
    private readonly ChargeViews _charges = new();
    private readonly ImpactViews _impacts = new();
    private readonly Node3D?[] _fighterNodes = new Node3D?[MatchState.PlayerCount];
    private readonly IFighterArt?[] _fighterArt = new IFighterArt?[MatchState.PlayerCount];
    private IMatchDriver<MatchState>? _driver;
    private CharacterData[] _characters = [];
    private LogicScreen _screen;
    private DeveloperToggle? _hitboxOverlay;
    private MatchEffects? _effects;
    private StageCamera? _camera;
    private System.Func<Sim.Events.FrameEvent, bool>? _sparkDrawnElsewhere;

    /// <param name="characters">P1's and P2's character, for their boxes and move names.</param>
    /// <param name="screen">The logic camera's width and the stage walls, from the match rules.</param>
    /// <param name="hitboxOverlay">Draws every box when on (developer setting).</param>
    /// <param name="effects">The match's event effects; its sounds join the scene as a child of this view.</param>
    public void Show(
        IMatchDriver<MatchState> driver,
        CharacterData[] characters,
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
        _hud.Show(driver, characters);
        _stage.Build(screen.Stage);
        _worldCamera.UsePerspective = _stage.WantsPerspective;
        ProjectileArtSettings?[][] projectileArt = ProjectileArtLookup.Of(characters);
        _projectiles.Build(characters, projectileArt);
        _charges.Build(characters, projectileArt);
        _impacts.Build(projectileArt);
        _sparkDrawnElsewhere = _impacts.HasArt;
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            _fighterNodes[player] =
                FighterArtChoice.Create(characters[player], _fighterColors[player], out IFighterArt art);
            _fighterArt[player] = art;
        }
    }

    public override void _Ready()
    {
        AddChild(_stage);
        AddChild(_worldCamera);
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
        StageCamera fitted = StageCamera.Fit(size, _screen, state.Camera.X);
        StageCamera camera = fitted with { FloorCentre = fitted.FloorCentre + _effects!.Shake.Offset(state.Frame - 1) };
        _camera = camera;
        CameraFraming framing = CameraFraming.Of(camera, size);
        _worldCamera.Frame(framing);
        _stage.FollowCamera(framing.Centre.X);
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            ref readonly FighterState fighter = ref state.Fighters[player];
            CollectBoxes(in fighter, _characters[player]);
            FighterPose pose = FighterPose.Of(_characters[player], in fighter) with { MatchFrame = state.Frame };
            _fighterArt[player]!.ShowPose(pose, fighter.Position, fighter.Facing, _boxes);
        }

        _charges.ShowFrame(in state.Fighters);
        _projectiles.ShowFrame(in state.Entities);
        int latestFrame = state.Frame - 1;
        _impacts.ShowFrame(_effects.Impacts.Live(latestFrame), latestFrame, in state.Fighters);

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
            DrawReadouts(camera, in state.Fighters[player], _characters[player], _fighterColors[player]);
        }

        int latestFrame = state.Frame - 1;
        HitSparkArt.Draw(this, camera, _effects!.Sparks.Live(latestFrame), latestFrame, _sparkDrawnElsewhere);
    }

    private void CollectBoxes(in FighterState fighter, CharacterData character)
    {
        _boxes.Clear();
        FighterBoxes.Collect(character, in fighter, _boxes);
    }

    private void DrawReadouts(StageCamera camera, in FighterState fighter, CharacterData character, Color color)
    {
        CollectBoxes(in fighter, character);
        FighterReadoutArt.Draw(this, camera, FighterPose.Of(character, in fighter), _boxes, color);
        if (_hitboxOverlay!.Enabled)
        {
            BoxOverlayArt.Draw(this, camera, _boxes);
        }
    }
}
