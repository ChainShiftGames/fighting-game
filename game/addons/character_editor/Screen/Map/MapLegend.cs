#if TOOLS
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>The map's key, bottom left: what a lit and a plain arrow mean.</summary>
[Tool]
public partial class MapLegend : Control
{
    private const string Lit = "can interrupt into (selected move)";
    private const string Plain = "other chains";
    private const float Height = 28f;
    private const float Sample = 34f;

    public MapLegend()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.BottomLeft);
        GrowVertical = GrowDirection.Begin;
    }

    public override void _Ready() => Measure();

    public override void _Notification(int what)
    {
        if (what == NotificationThemeChanged)
        {
            Measure();
        }
    }

    public override void _Draw()
    {
        MapLook look = MapLook.Of(this);
        ShapeArt.Panel(this, new Rect2(Vector2.Zero, Size), MapPalette.Background with { A = 0.8f }, look.Px(8f));
        float x = look.Px(10f);
        x = Item(look, x, Lit, MapPalette.Glow, MapPalette.Glow);
        Item(look, x + look.Px(16f), Plain, MapPalette.EdgeDim, MapPalette.ArrowDim);
    }

    private float Item(MapLook look, float x, string text, Color line, Color head)
    {
        float middle = Size.Y / 2f;
        SampleLine(look, x, middle, line, head);
        x += look.Px(Sample + 6f);
        int size = look.FontSize(12f);
        DrawString(look.Body, new Vector2(x, middle + (look.Body.GetAscent(size) / 2.6f)), text,
            HorizontalAlignment.Left, -1f, size, MapPalette.Muted);
        return x + look.Body.GetStringSize(text, HorizontalAlignment.Left, -1f, size).X;
    }

    private void SampleLine(MapLook look, float x, float middle, Color line, Color head)
    {
        DrawLine(new Vector2(x, middle), new Vector2(x + look.Px(Sample - 6f), middle), line, look.Px(2f), true);
        ShapeArt.Arrowhead(this, new Vector2(x + look.Px(Sample), middle), Vector2.Right, look.Px(7f), head);
    }

    private void Measure()
    {
        MapLook look = MapLook.Of(this);
        int size = look.FontSize(12f);
        float text = look.Body.GetStringSize(Lit + Plain, HorizontalAlignment.Left, -1f, size).X;
        CustomMinimumSize = new Vector2(text + look.Px((2f * (Sample + 6f)) + 36f), look.Px(Height));
        OffsetLeft = look.Px(18f);
        OffsetBottom = -look.Px(14f);
        OffsetTop = OffsetBottom - CustomMinimumSize.Y;
        OffsetRight = OffsetLeft + CustomMinimumSize.X;
        QueueRedraw();
    }
}
#endif
