using Godot;

namespace FightingGame.Presentation.Hud;

/// <summary>Where each HUD piece goes on a screen of a given size: designed at 1280 × 720 and scaled to fit, P2 mirroring P1.</summary>
/// <remarks>Plain maths on Godot's value types, so it runs without the engine (tests).</remarks>
public readonly struct HudLayout
{
    private static readonly Vector2 _reference = new(1280f, 720f);

    // In reference pixels.
    private const float Top = 31f;
    private const float Side = 38f;
    private const float TimerSize = 95f;
    private const float TimerRaise = 15f;
    private const float ColumnGap = 15f;
    private const float BarHeight = 24f;
    private const float BurstHeight = 6f;
    private const float BurstShare = 0.7f;
    private const float RowGap = 8f;
    private const float NameRowHeight = 20f;
    private const float Bottom = 31f;
    private const float SegmentWidth = 154f;
    private const float SegmentHeight = 17f;
    private const float SegmentGap = 6f;
    private const float LabelHeight = 14f;
    private const float LabelGap = 5f;
    private const float MeterNumberWidth = 50f;
    private const float MeterNumberGap = 13f;
    private const float ComboTop = 192f;
    private const float BannerBaseline = 300f;

    private readonly Vector2 _screen;
    private readonly Vector2 _origin;

    private HudLayout(Vector2 screen)
    {
        _screen = screen;
        Scale = Mathf.Min(screen.X / _reference.X, screen.Y / _reference.Y);

        // A screen wider or taller than 16:9 keeps the HUD at 16:9 in its middle.
        _origin = (screen - (_reference * Scale)) / 2f;
    }

    /// <summary>Screen pixels per reference pixel.</summary>
    public float Scale { get; }

    /// <summary>Segments in the meter: one per super's cost.</summary>
    public static int MeterSegments => 2;

    public static HudLayout For(Vector2 screen) => new(screen);

    public Rect2 Timer => Place(new Rect2((_reference.X - TimerSize) / 2f, Top - TimerRaise, TimerSize, TimerSize));

    public Rect2 HealthBar(int player) => Mirrored(player, new Rect2(Side, Top, BarWidth, BarHeight));

    /// <summary>The burst gauge: a thin line under the health bar, against its inner end.</summary>
    public Rect2 BurstLine(int player)
    {
        float width = BarWidth * BurstShare;
        return Mirrored(player, new Rect2(Side + BarWidth - width, Top + BarHeight + RowGap, width, BurstHeight));
    }

    /// <summary>The row with the name and round wins, the full width of the bar; P1 reads from the left, P2 from the right.</summary>
    public Rect2 NameRow(int player) =>
        Mirrored(player, new Rect2(Side, Top + BarHeight + RowGap + BurstHeight + RowGap, BarWidth, NameRowHeight));

    /// <summary>One meter segment; segment 0 is the outer one, which fills first.</summary>
    public Rect2 MeterSegment(int player, int segment)
    {
        float left = Side + MeterNumberWidth + MeterNumberGap + (segment * (SegmentWidth + SegmentGap));
        return Mirrored(player, new Rect2(left, MeterTop, SegmentWidth, SegmentHeight));
    }

    /// <summary>The "TENSION" label above the segments.</summary>
    public Rect2 MeterLabel(int player) => Mirrored(
        player,
        new Rect2(Side + MeterNumberWidth + MeterNumberGap, MeterTop - LabelGap - LabelHeight, SegmentWidth, LabelHeight));

    /// <summary>The meter's percentage, outside the segments, bottom-aligned with them.</summary>
    public Rect2 MeterNumber(int player) => Mirrored(
        player, new Rect2(Side, MeterTop + SegmentHeight - (2f * SegmentHeight), MeterNumberWidth, 2f * SegmentHeight));

    /// <summary>The top outer corner of the combo counter, on the attacker's side.</summary>
    public Rect2 Combo(int player) => Mirrored(player, new Rect2(Side, ComboTop, BarWidth / 2f, 0f));

    /// <summary>The middle of the round banner's headline baseline (KO, time-out, the winner).</summary>
    public Vector2 Banner => _origin + (new Vector2(_reference.X / 2f, BannerBaseline) * Scale);

    /// <summary>A font size given in reference pixels, on this screen.</summary>
    public int FontSize(float referenceSize) => Mathf.Max(1, Mathf.RoundToInt(referenceSize * Scale));

    private static float BarWidth => (_reference.X - (2f * Side) - TimerSize - (2f * ColumnGap)) / 2f;

    private static float MeterTop => _reference.Y - Bottom - SegmentHeight;

    private Rect2 Mirrored(int player, Rect2 p1Rect)
    {
        Rect2 rect = player == 0
            ? p1Rect
            : p1Rect with { Position = new Vector2(_reference.X - p1Rect.End.X, p1Rect.Position.Y) };
        return Place(rect);
    }

    private Rect2 Place(Rect2 reference) => new(_origin + (reference.Position * Scale), reference.Size * Scale);
}
