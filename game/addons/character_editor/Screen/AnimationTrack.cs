#if TOOLS
using System;
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The animation track under the <see cref="FrameTimeline"/> (ADR-0028): a line of the clip frame each move frame
/// shows, with its pins: the contact frames on the first active and recovery frames, and timing keys. Drag a pin up or
/// down to change its clip frame, double-click a frame to pin it where it is, right-click a key to remove it; a
/// click picks the frame, as on the timeline.
/// </summary>
[Tool]
public partial class AnimationTrack : Control
{
    // 1× design pixels (MapLook); frames line up with the timeline's columns.
    private const float PlotHeight = 84f;
    private const float PlotPad = 6f;
    private const float PinRadius = 4f;
    private const float FontSize = 11f;

    private static readonly Color _line = MapPalette.Glow;
    private const float PhaseShade = 0.07f;
    private static readonly Color _marker = new(1f, 1f, 1f, 0.8f);

    private AnimationTrackData? _data;
    private int _markedFrame;
    private int _dragged;

    /// <summary>A frame was clicked.</summary>
    public event Action<int>? FrameClicked;

    /// <summary>A frame was pinned to a clip frame (by a drag, or a double-click where it is).</summary>
    public event Action<int, int>? Pinned;

    /// <summary>A key was removed from a frame.</summary>
    public event Action<int>? Unpinned;

    public void ShowTrack(AnimationTrackData? data)
    {
        _data = data;
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
        if (what == NotificationThemeChanged)
        {
            FitSize();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_data is null)
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true } press
                when FrameAt(press.Position) is int frame && _data.PinOn(frame) is null:
                Pinned?.Invoke(frame, _data.ClipAt(frame));
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press
                when FrameAt(press.Position) is int frame:
                _dragged = _data.PinOn(frame) is not null ? frame : 0;
                FrameClicked?.Invoke(frame);
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                _dragged = 0;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } press
                when FrameAt(press.Position) is int frame && _data.PinOn(frame) is { Kind: TrackPinKind.Key }:
                Unpinned?.Invoke(frame);
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragged > 0:
                int clip = ClipAt(motion.Position.Y);
                if (_data.PinOn(_dragged) is { } pin && pin.Clip != clip)
                {
                    Pinned?.Invoke(_dragged, clip);
                }

                AcceptEvent();
                break;
        }
    }

    public override string _GetTooltip(Vector2 atPosition)
    {
        if (_data is null || FrameAt(atPosition) is not int frame)
        {
            return "";
        }

        string shows = string.Create(CultureInfo.InvariantCulture, $"Frame {frame} shows clip frame {_data.ClipFrames[frame - 1]:0.#}");
        return _data.PinOn(frame) switch
        {
            { Kind: TrackPinKind.ActiveFrom } => $"{shows}: the contact pose (activeFrom). Drag up or down to change it.",
            { Kind: TrackPinKind.RecoveryFrom } => $"{shows}: recovery starts (recoveryFrom). Drag up or down to change it.",
            { Kind: TrackPinKind.Key } => $"{shows}: a timing key. Drag up or down to change it; right-click removes it.",
            _ => frame == 1 ? $"{shows}: the clip's start (clipFrom)." : $"{shows}. Double-click to pin it.",
        };
    }

    public override void _Draw()
    {
        if (_data is null)
        {
            return;
        }

        MapLook look = MapLook.Of(this);
        DrawText(look, look.Body, MapPalette.TextSoft, "Animation", 0f);
        DrawText(look, look.Mono, MapPalette.Muted,
            string.Create(CultureInfo.InvariantCulture, $"clip {_data.Part.Start}–{_data.Part.End}"), look.Px(FontSize + 4f));
        DrawPhases(look);
        if (_markedFrame >= 1 && _markedFrame <= _data.ClipFrames.Count)
        {
            float x = CellCentre(look, _markedFrame);
            DrawLine(new Vector2(x, 0f), new Vector2(x, Size.Y), _marker, look.Px(1f));
        }

        var points = new Vector2[_data.ClipFrames.Count];
        for (int frame = 1; frame <= points.Length; frame++)
        {
            points[frame - 1] = new Vector2(CellCentre(look, frame), Y(look, _data.ClipFrames[frame - 1]));
        }

        if (points.Length > 1)
        {
            DrawPolyline(points, _line, look.Px(1.5f), antialiased: true);
        }

        foreach (TrackPin pin in _data.Pins)
        {
            DrawPin(look, pin, points[pin.Frame - 1]);
        }
    }

    private void DrawPhases(MapLook look)
    {
        FramePhases phases = _data!.Phases;
        for (int frame = 1; frame <= _data.ClipFrames.Count; frame++)
        {
            Color phase = frame < phases.FirstActiveFrame ? MapPalette.Windup
                : frame <= phases.LastActiveFrame ? MapPalette.Active
                : MapPalette.Recovery;
            float left = look.Px(FrameTimeline.LabelWidth + ((frame - 1) * (FrameTimeline.CellWidth + FrameTimeline.CellGap)));
            DrawRect(new Rect2(left, 0f, look.Px(FrameTimeline.CellWidth), Size.Y), phase with { A = PhaseShade });
        }
    }

    private void DrawPin(MapLook look, TrackPin pin, Vector2 at)
    {
        float radius = look.Px(PinRadius);
        if (pin.Kind == TrackPinKind.Key)
        {
            DrawCircle(at, radius, MapPalette.Text);
            DrawArc(at, radius, 0f, Mathf.Tau, 16, MapPalette.Background, look.Px(1f), antialiased: true);
            return;
        }

        // Contact frames are diamonds in their phase's colour: what the hit and the recovery line up with.
        Color color = pin.Kind == TrackPinKind.ActiveFrom ? MapPalette.Active : MapPalette.Recovery;
        Vector2[] diamond = [at + new Vector2(0f, -radius - 1f), at + new Vector2(radius + 1f, 0f),
            at + new Vector2(0f, radius + 1f), at + new Vector2(-radius - 1f, 0f)];
        DrawColoredPolygon(diamond, color);
    }

    private float Y(MapLook look, float clip)
    {
        ClipPart part = _data!.Part;
        float span = Mathf.Max(1, part.End - part.Start);
        float share = Mathf.Clamp((clip - part.Start) / span, 0f, 1f);
        return look.Px(PlotPad) + ((1f - share) * look.Px(PlotHeight - (2f * PlotPad)));
    }

    private int ClipAt(float y)
    {
        MapLook look = MapLook.Of(this);
        ClipPart part = _data!.Part;
        float share = 1f - ((y - look.Px(PlotPad)) / look.Px(PlotHeight - (2f * PlotPad)));
        return part.Clamp(part.Start + Mathf.RoundToInt(share * (part.End - part.Start)));
    }

    private int? FrameAt(Vector2 position)
    {
        MapLook look = MapLook.Of(this);
        float left = look.Px(FrameTimeline.LabelWidth);
        int frame = (int)((position.X - left) / look.Px(FrameTimeline.CellWidth + FrameTimeline.CellGap)) + 1;
        return _data is not null && position.X >= left && frame <= _data.ClipFrames.Count ? frame : null;
    }

    private void FitSize()
    {
        MapLook look = MapLook.Of(this);
        int frames = _data?.ClipFrames.Count ?? 0;
        CustomMinimumSize = _data is null
            ? Vector2.Zero
            : new Vector2(look.Px(FrameTimeline.LabelWidth + (frames * (FrameTimeline.CellWidth + FrameTimeline.CellGap))),
                look.Px(PlotHeight));
    }

    private static float CellCentre(MapLook look, int frame) => look.Px(FrameTimeline.LabelWidth +
        ((frame - 1) * (FrameTimeline.CellWidth + FrameTimeline.CellGap)) + (FrameTimeline.CellWidth / 2f));

    private void DrawText(MapLook look, Font font, Color color, string text, float top) => DrawString(
        font, new Vector2(0f, top + look.Px(FontSize + 1f)), text, HorizontalAlignment.Left, -1f, look.FontSize(FontSize), color);
}
#endif
