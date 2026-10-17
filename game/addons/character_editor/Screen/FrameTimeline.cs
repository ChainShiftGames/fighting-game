#if TOOLS
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// Draws a <see cref="MoveTimeline"/>: one cell per frame, a row for the phases and for each box kind, cancel
/// window, velocity and projectile spawns. Hovering a cell shows its frame number.
/// </summary>
[Tool]
public partial class FrameTimeline : Control
{
    private const float LabelWidth = SchemaForm.LabelWidth;
    private const float CellWidth = 10f;
    private const float CellGap = 1f;
    private const float RowHeight = 16f;
    private const float RowGap = 3f;
    private const float HeaderHeight = 16f;
    private const int FontSize = 12;
    private const int FrameNumberEvery = 5;

    private static readonly Color _emptyCell = new(0.18f, 0.18f, 0.2f);
    private static readonly Color _textColor = new(0.85f, 0.85f, 0.9f);
    private static readonly Color _outsideMove = new(1f, 1f, 1f, 0.35f);

    private MoveTimeline? _timeline;

    public void ShowTimeline(MoveTimeline? timeline)
    {
        _timeline = timeline;
        int rows = timeline?.Rows.Count ?? 0;
        int frames = timeline?.FrameCount ?? 0;
        CustomMinimumSize = new Vector2(
            LabelWidth + (frames * (CellWidth + CellGap)), HeaderHeight + (rows * (RowHeight + RowGap)));
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_timeline is null)
        {
            return;
        }

        for (int frame = 1; frame <= _timeline.FrameCount; frame++)
        {
            if (frame == 1 || frame % FrameNumberEvery == 0)
            {
                DrawText(frame.ToString(System.Globalization.CultureInfo.InvariantCulture), CellLeft(frame), 0f);
            }
        }

        int moveFrames = _timeline.Phases?.TotalFrames ?? 0;
        for (int row = 0; row < _timeline.Rows.Count; row++)
        {
            float top = HeaderHeight + (row * (RowHeight + RowGap));
            DrawText(_timeline.Rows[row].Label, 0f, top);
            for (int frame = 1; frame <= _timeline.FrameCount; frame++)
            {
                DrawRect(Cell(frame, top), _emptyCell);
            }

            foreach (TimelineSpan span in _timeline.Rows[row].Spans)
            {
                for (int frame = span.FirstFrame; frame <= span.LastFrame; frame++)
                {
                    if (frame < 1)
                    {
                        continue;
                    }

                    DrawRect(Cell(frame, top), ColorOf(span.Kind));
                    if (frame > moveFrames)
                    {
                        // Past the move's last frame: validation reports it; the outline shows where.
                        DrawRect(Cell(frame, top), _outsideMove, filled: false, width: 1f);
                    }
                }
            }
        }
    }

    public override string _GetTooltip(Vector2 atPosition)
    {
        int frame = (int)((atPosition.X - LabelWidth) / (CellWidth + CellGap)) + 1;
        return _timeline is not null && atPosition.X >= LabelWidth && frame <= _timeline.FrameCount
            ? $"Frame {frame}"
            : "";
    }

    private static float CellLeft(int frame) => LabelWidth + ((frame - 1) * (CellWidth + CellGap));

    private static Rect2 Cell(int frame, float top) => new(CellLeft(frame), top, CellWidth, RowHeight);

    private void DrawText(string text, float left, float top) => DrawString(
        ThemeDB.FallbackFont, new Vector2(left, top + FontSize), text, HorizontalAlignment.Left, -1f, FontSize, _textColor);

    // Phases use the training frame meter's colours (FrameMeterBar), so both read the same way.
    private static Color ColorOf(TimelineSpanKind kind) => kind switch
    {
        TimelineSpanKind.Startup => new Color(0.2f, 0.75f, 0.35f),
        TimelineSpanKind.Active => new Color(0.9f, 0.25f, 0.25f),
        TimelineSpanKind.Recovery => new Color(0.25f, 0.45f, 0.95f),
        TimelineSpanKind.Loop => new Color(0.55f, 0.45f, 0.75f),
        TimelineSpanKind.Invulnerable => new Color(1f, 1f, 1f),
        TimelineSpanKind.HurtBox => new Color(0.3f, 0.7f, 0.95f),
        TimelineSpanKind.HitBox => new Color(0.95f, 0.3f, 0.3f),
        TimelineSpanKind.PushBox => new Color(0.95f, 0.85f, 0.3f),
        TimelineSpanKind.ThrowBox => new Color(0.85f, 0.4f, 0.9f),
        TimelineSpanKind.Cancel => new Color(0.3f, 0.85f, 0.75f),
        TimelineSpanKind.Velocity => new Color(0.95f, 0.6f, 0.2f),
        TimelineSpanKind.ProjectileSpawn => new Color(1f, 0.5f, 0.7f),
        _ => new Color(0.6f, 0.6f, 0.6f),
    };
}
#endif
