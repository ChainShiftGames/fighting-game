using System.Collections.Generic;
using FightingGame.Debug;
using Godot;
using Netcode.Drivers;
using Sim.Characters;
using Sim.State;

namespace FightingGame.Presentation;

/// <summary>
/// Draws a match of the real game logic from the driver's state: stage, placeholder fighters, the hitbox overlay
/// when it is on, and the HUD. Reads the state only (ARCHITECTURE.md §9).
/// </summary>
public partial class MatchView : Node2D
{
    private const float FloorWidth = 2f;
    private const float CentreMarkHeight = 6f;

    private static readonly Color _floorColor = new(0.35f, 0.35f, 0.4f);
    private static readonly Color _backgroundColor = new(0.08f, 0.08f, 0.1f);

    // Blue and orange: told apart with the common kinds of colour blindness (ADR-0008 §5).
    private static readonly Color[] _fighterColors = [new(0.25f, 0.55f, 1f), new(1f, 0.6f, 0.15f)];

    private readonly List<WorldBox> _boxes = [];
    private readonly MatchHud _hud = new();
    private IMatchDriver<MatchState>? _driver;
    private CharacterData[] _characters = [];
    private DeveloperToggle? _hitboxOverlay;

    /// <param name="characters">P1's and P2's character, for their boxes and move names.</param>
    /// <param name="hitboxOverlay">Draws every box when on (developer setting).</param>
    public void Show(IMatchDriver<MatchState> driver, CharacterData[] characters, DeveloperToggle hitboxOverlay)
    {
        _driver = driver;
        _characters = characters;
        _hitboxOverlay = hitboxOverlay;
        _hud.Show(driver, characters);
        QueueRedraw();
    }

    public override void _Ready() => AddChild(_hud);

    public override void _Draw()
    {
        if (_driver is null)
        {
            return;
        }

        Vector2 size = GetViewportRect().Size;
        DrawRect(new Rect2(Vector2.Zero, size), _backgroundColor);
        StageCamera camera = StageCamera.Fit(size);
        DrawStage(camera, size);

        ref readonly MatchState state = ref _driver.State;
        for (int player = 0; player < MatchState.PlayerCount; player++)
        {
            DrawFighter(camera, in state.Fighters[player], _characters[player], _fighterColors[player]);
        }
    }

    // Walls come with P1-10 (collision, walls and corners), which puts the stage width in the match rules.
    private void DrawStage(StageCamera camera, Vector2 size)
    {
        Vector2 centre = camera.FloorCentre;
        DrawLine(new Vector2(0f, centre.Y), new Vector2(size.X, centre.Y), _floorColor, FloorWidth);
        DrawLine(centre, centre + new Vector2(0f, CentreMarkHeight), _floorColor, FloorWidth);
    }

    private void DrawFighter(StageCamera camera, in FighterState fighter, CharacterData character, Color color)
    {
        _boxes.Clear();
        FighterBoxes.Collect(character, in fighter, _boxes);
        PlaceholderFighterArt.Draw(this, camera, FighterPose.Of(character, in fighter), fighter.Facing, _boxes, color);
        if (_hitboxOverlay!.Enabled)
        {
            BoxOverlayArt.Draw(this, camera, _boxes);
        }
    }
}
