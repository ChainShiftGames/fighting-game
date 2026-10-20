#if TOOLS
using System;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Look;

/// <summary>
/// The one call to action on screen (Save): a rounded cyan-to-violet button, drawn by hand because style boxes
/// can't fill with a gradient. A dot marks unsaved changes.
/// </summary>
[Tool]
public partial class GradientButton : Control
{
    private const float Height = 34f;
    private const float PadX = 16f;
    private const float Radius = 9f;
    private const float FontSize = 13f;
    private string _text = "";
    private bool _hovered;
    private bool _disabled;

    public GradientButton()
    {
        MouseDefaultCursorShape = CursorShape.PointingHand;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseEntered += () => SetHover(true);
        MouseExited += () => SetHover(false);
    }

    public event Action? Pressed;

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            Measure();
        }
    }

    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            MouseDefaultCursorShape = value ? CursorShape.Arrow : CursorShape.PointingHand;
            QueueRedraw();
        }
    }

    /// <summary>Shows the unsaved-changes dot.</summary>
    public bool Marked { get; set; }

    public override void _Ready() => Measure();

    public override void _Draw()
    {
        MapLook look = MapLook.Of(this);
        float alpha = _disabled ? 0.35f : _hovered ? 1f : 0.92f;
        var rect = new Rect2(Vector2.Zero, Size);
        ShapeArt.Gradient(this, rect, look.Px(Radius), MapPalette.Glow with { A = alpha }, MapPalette.Violet with { A = alpha });
        int size = look.FontSize(FontSize);
        float baseline = (Size.Y + look.Bold.GetAscent(size) - look.Bold.GetDescent(size)) / 2f;
        DrawString(look.Bold, new Vector2(0f, baseline), _text, HorizontalAlignment.Center, Size.X, size,
            MapPalette.OnGlow with { A = _disabled ? 0.6f : 1f });
        if (Marked && !_disabled)
        {
            DrawCircle(new Vector2(Size.X - look.Px(8f), look.Px(8f)), look.Px(3.5f), MapPalette.OnGlow);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } && !_disabled
            && _hovered)
        {
            Pressed?.Invoke();
            AcceptEvent();
        }
    }

    private void SetHover(bool hovered)
    {
        _hovered = hovered;
        QueueRedraw();
    }

    private void Measure()
    {
        MapLook look = MapLook.Of(this);
        float width = look.Bold.GetStringSize(_text, HorizontalAlignment.Left, -1f, look.FontSize(FontSize)).X;
        CustomMinimumSize = new Vector2(width + look.Px(2f * PadX), look.Px(Height));
        QueueRedraw();
    }
}
#endif
