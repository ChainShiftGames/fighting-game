using Godot;
using Netcode.Drivers;
using Sim.Toy;

namespace FightingGame.Presentation;

/// <summary>Draws the Spike A toy match (two squares on a floor) from the driver's state. Placeholder art.</summary>
public partial class ToyMatchView : Node2D
{
    private const float SubpixelsPerPixel = 1000f;
    private const float FighterSize = 64f;

    // Floor line as a share of the window height, from the top.
    private const float FloorHeight = 0.8f;

    private static readonly Color _floorColor = new(0.35f, 0.35f, 0.4f);
    private static readonly Color[] _fighterColors = [new(0.25f, 0.55f, 1f), new(1f, 0.35f, 0.3f)];

    private IMatchDriver<ToyState>? _driver;
    private int _stageHalfWidth;

    public void Show(IMatchDriver<ToyState> driver, ToyRules rules)
    {
        _driver = driver;
        _stageHalfWidth = rules.StageHalfWidth;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_driver is null)
        {
            return;
        }

        Vector2 size = GetViewportRect().Size;
        var floor = new Vector2(size.X / 2f, size.Y * FloorHeight);
        var floorHalf = new Vector2((_stageHalfWidth / SubpixelsPerPixel) + FighterSize, 0f);
        DrawLine(floor - floorHalf, floor + floorHalf, _floorColor, 2f);

        ref readonly ToyState state = ref _driver.State;
        for (int i = 0; i < ToyFighters.Count; i++)
        {
            ToyFighter fighter = state.Fighters[i];
            var feet = floor + new Vector2(fighter.PositionX, -fighter.PositionY) / SubpixelsPerPixel;
            var topLeft = feet - new Vector2(FighterSize / 2f, FighterSize);
            DrawRect(new Rect2(topLeft, new Vector2(FighterSize, FighterSize)), _fighterColors[i]);
        }
    }
}
