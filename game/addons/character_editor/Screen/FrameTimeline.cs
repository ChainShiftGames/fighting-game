#if TOOLS
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// Draws a <see cref="MoveTimeline"/>: one cell per frame, a row for the phases and for each box kind, cancel
/// window, velocity and projectile spawns. Hovering a cell shows its frame number; clicking picks that frame, which
/// is marked (the box editor's frame).
/// </summary>
[Tool]
public partial class FrameTimeline : Control
{
    // 1× design pixels (MapLook); the drawer is narrow, so labels get less room than in a form.
    private const float LabelWidth = 92f;
    private const float CellWidth = 10f;
    private const float CellGap = 1f;
    private const float RowHeight = 16f;
    private const float RowGap = 3f;
    private const float HeaderHeight = 16f;
    private const float FontSize = 11f;
    private const int FrameNumberEvery = 5;

    private static readonly Color _outsideMove = new(1f, 1f, 1f, 0.35f);
    private static readonly Color _marker = new(1f, 1f, 1f, 0.8f);

    private MoveTimeline? _timeline;
    private int _markedFrame;

    /// <summary>A frame was clicked.</summary>
    public event System.Action<int>? FrameClicked;

    public void ShowTimeline(MoveTimeline? timeline)
    {
        _timeline = timeline;
        FitSize();
        QueueRedraw();
    }

    /// <summary>Outlines the frame's column; 0 for none.</summary>
    public void MarkFrame(int frame)
    {
        _markedFrame = frame;
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        // The scale comes from the screen's theme, which this control only sees once it is under the screen.
        if (what == NotificationThemeChanged)
        {
            FitSize();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click
            && FrameAt(click.Position) is int frame)
        {
            FrameClicked?.Invoke(frame);
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        if (_timeline is null)
        {
            return;
        }

        MapLook look = MapLook.Of(this);
        if (_markedFrame >= 1 && _markedFrame <= _timeline.FrameCount)
        {
            float top = look.Px(HeaderHeight - RowGap);
            DrawRect(new Rect2(CellLeft(look, _markedFrame) - look.Px(CellGap), top,
                look.Px(CellWidth + (2f * CellGap)), Size.Y - top), _marker, filled: false, width: look.Px(1f));
        }

        for (int frame = 1; frame <= _timeline.FrameCount; frame++)
        {
            if (frame == 1 || frame % FrameNumberEvery == 0)
            {
                DrawText(look, look.Mono, MapPalette.Muted,
                    frame.ToString(System.Globalization.CultureInfo.InvariantCulture), CellLeft(look, frame), 0f);
            }
        }

        int moveFrames = _timeline.Phases?.TotalFrames ?? 0;
        for (int row = 0; row < _timeline.Rows.Count; row++)
        {
            float top = look.Px(HeaderHeight + (row * (RowHeight + RowGap)));
            DrawText(look, look.Body, MapPalette.TextSoft, _timeline.Rows[row].Label, 0f, top);
            for (int frame = 1; frame <= _timeline.FrameCount; frame++)
            {
                DrawRect(Cell(look, frame, top), MapPalette.Raised);
            }

            foreach (TimelineSpan span in _timeline.Rows[row].Spans)
            {
                for (int frame = span.FirstFrame; frame <= span.LastFrame; frame++)
                {
                    if (frame < 1)
                    {
                        continue;
                    }

                    DrawRect(Cell(look, frame, top), ColorOf(span.Kind));
                    if (frame > moveFrames)
                    {
                        // Past the move's last frame: validation reports it; the outline shows where.
                        DrawRect(Cell(look, frame, top), _outsideMove, filled: false, width: look.Px(1f));
                    }
                }
            }
        }
    }

    public override string _GetTooltip(Vector2 atPosition) => FrameAt(atPosition) is int frame ? $"Frame {frame}" : "";

    private int? FrameAt(Vector2 position)
    {
        MapLook look = MapLook.Of(this);
        int frame = (int)((position.X - look.Px(LabelWidth)) / look.Px(CellWidth + CellGap)) + 1;
        return _timeline is not null && position.X >= look.Px(LabelWidth) && frame <= _timeline.FrameCount
            ? frame
            : null;
    }

    private void FitSize()
    {
        MapLook look = MapLook.Of(this);
        int rows = _timeline?.Rows.Count ?? 0;
        int frames = _timeline?.FrameCount ?? 0;
        CustomMinimumSize = new Vector2(look.Px(LabelWidth + (frames * (CellWidth + CellGap))),
            look.Px(HeaderHeight + (rows * (RowHeight + RowGap))));
    }

    private static float CellLeft(MapLook look, int frame) =>
        look.Px(LabelWidth + ((frame - 1) * (CellWidth + CellGap)));

    private static Rect2 Cell(MapLook look, int frame, float top) =>
        new(CellLeft(look, frame), top, look.Px(CellWidth), look.Px(RowHeight));

    private void DrawText(MapLook look, Font font, Color color, string text, float left, float top) => DrawString(
        font, new Vector2(left, top + look.Px(FontSize + 1f)), text, HorizontalAlignment.Left, -1f,
        look.FontSize(FontSize), color);

    // Phases use the editor's phase colours, so the timeline, the frame strip and the map cards read the same way.
    private static Color ColorOf(TimelineSpanKind kind) => kind switch
    {
        TimelineSpanKind.Startup => MapPalette.Windup,
        TimelineSpanKind.Active => MapPalette.Active,
        TimelineSpanKind.Recovery => MapPalette.Recovery,
        TimelineSpanKind.Loop => MapPalette.Loop,
        TimelineSpanKind.Invulnerable => new Color(1f, 1f, 1f),
        TimelineSpanKind.HurtBox => new Color(0.3f, 0.7f, 0.95f),
        TimelineSpanKind.HitBox => new Color(0.95f, 0.3f, 0.3f),
        TimelineSpanKind.PushBox => new Color(0.95f, 0.85f, 0.3f),
        TimelineSpanKind.ThrowBox => new Color(0.85f, 0.4f, 0.9f),
        TimelineSpanKind.Cancel => MapPalette.Glow,
        TimelineSpanKind.Velocity => new Color(0.95f, 0.6f, 0.2f),
        TimelineSpanKind.ProjectileSpawn => new Color(1f, 0.5f, 0.7f),
        _ => MapPalette.Muted,
    };
}
#endif
