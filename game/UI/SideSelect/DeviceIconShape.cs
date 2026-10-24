using Godot;

namespace FightingGame.UI.SideSelect;

/// <summary>A generic keyboard, gamepad or arcade-stick picture drawn from shapes; UI-11 swaps in per-brand art.</summary>
public partial class DeviceIconShape : Control
{
    private const float Width = 56f;
    private const float Height = 40f;
    private const float Line = 2f;

    private static readonly Color _ink = new(1f, 1f, 1f, 0.9f);
    private DeviceIcon _icon;

    public DeviceIcon Icon
    {
        get => _icon;
        set
        {
            if (_icon != value)
            {
                _icon = value;
                QueueRedraw();
            }
        }
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(Width, Height);
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        switch (_icon)
        {
            case DeviceIcon.Keyboard:
                DrawKeyboard();
                break;
            case DeviceIcon.ArcadeStick:
                DrawArcadeStick();
                break;
            default:
                DrawGamepad();
                break;
        }
    }

    private void DrawKeyboard()
    {
        DrawRect(new Rect2(2f, 8f, 52f, 26f), _ink, filled: false, Line);
        for (int row = 0; row < 3; row++)
        {
            for (int key = 0; key < 7; key++)
            {
                DrawRect(new Rect2(6f + (key * 6.5f), 12f + (row * 6.5f), 4f, 4f), _ink);
            }
        }
    }

    private void DrawGamepad()
    {
        DrawRect(new Rect2(12f, 10f, 32f, 18f), _ink, filled: false, Line);
        DrawArc(new Vector2(12f, 22f), 10f, Mathf.Pi * 0.5f, Mathf.Pi * 1.5f, 12, _ink, Line);
        DrawArc(new Vector2(44f, 22f), 10f, -Mathf.Pi * 0.5f, Mathf.Pi * 0.5f, 12, _ink, Line);
        DrawRect(new Rect2(8f, 19f, 10f, 3f), _ink);
        DrawRect(new Rect2(11.5f, 15.5f, 3f, 10f), _ink);
        DrawCircle(new Vector2(42f, 17f), 2.2f, _ink);
        DrawCircle(new Vector2(47f, 22f), 2.2f, _ink);
        DrawCircle(new Vector2(37f, 22f), 2.2f, _ink);
        DrawCircle(new Vector2(42f, 27f), 2.2f, _ink);
    }

    private void DrawArcadeStick()
    {
        DrawRect(new Rect2(2f, 20f, 52f, 16f), _ink, filled: false, Line);
        DrawLine(new Vector2(14f, 20f), new Vector2(14f, 9f), _ink, Line);
        DrawCircle(new Vector2(14f, 8f), 5f, _ink);
        for (int button = 0; button < 4; button++)
        {
            DrawCircle(new Vector2(28f + (button * 7f), 26f + (button % 2 == 0 ? 0f : -2f)), 2.4f, _ink);
        }
    }
}
