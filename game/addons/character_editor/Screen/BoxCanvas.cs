#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Presentation;
using Godot;
using Sim.Characters;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// One frame of a move, facing right: the placeholder body the game draws, every box on the frame in the hitbox
/// overlay's colours, and the selected box with handles. Left-drag moves or resizes the selected box, or draws a
/// new box of <see cref="DrawKind"/> on empty space; right- or middle-drag pans; the wheel zooms.
/// </summary>
/// <remarks>Edges snap to whole pixels; hold Alt to place them on any subpixel.</remarks>
[Tool]
public partial class BoxCanvas : Control
{
    private const float Subpixels = 1000f;
    private const int PixelSnap = 1000;
    private const float StartZoom = 2f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 12f;
    private const float ZoomStep = 1.15f;
    private const float FloorFromTop = 0.85f;
    private const float HandleSize = 7f;
    private const float FillAlpha = 0.2f;
    private const float OutlineWidth = 1.5f;
    private const float SelectedWidth = 3f;

    private static readonly Color _background = new(0.12f, 0.12f, 0.14f);
    private static readonly Color _axis = new(1f, 1f, 1f, 0.25f);
    private static readonly Color _body = new(0.55f, 0.55f, 0.6f, 0.45f);
    private static readonly Color _unknownKind = new(0.6f, 0.6f, 0.6f);
    private static readonly Color _handle = new(1f, 1f, 1f);

    private JsonObject _move = [];
    private IReadOnlyList<string> _fieldOrder = [];
    private int _frame = 1;
    private float _zoom = StartZoom;
    private Vector2? _origin;

    // The drag in progress: what is dragged, from where (in subpixels), and the box's area when it started.
    private BoxHandle _handleDragged;
    private bool _drawing;
    private bool _panning;
    private bool _moved;
    private Vector2I _dragStart;
    private BoxArea _startArea;

    public BoxCanvas()
    {
        CustomMinimumSize = new Vector2(0f, 360f);
        ClipContents = true;
        FocusMode = FocusModeEnum.Click;
    }

    /// <summary>The selected box changed (null: none).</summary>
    public event Action<JsonObject?>? SelectionChanged;

    /// <summary>A drag changed the boxes: moved, resized, split out of a range or drawn.</summary>
    public event Action? Edited;

    public JsonObject? Selected { get; private set; }

    /// <summary>The kind a drag on empty space draws; null to only select.</summary>
    public string? DrawKind { get; set; }

    /// <summary>Dragging a box that spans several frames first splits this frame out of it.</summary>
    public bool ThisFrameOnly { get; set; } = true;

    /// <param name="fieldOrder">The move's field names in schema order, for a move without boxes.</param>
    public void ShowFrame(JsonObject move, IReadOnlyList<string> fieldOrder, int frame, JsonObject? selected)
    {
        _move = move;
        _fieldOrder = fieldOrder;
        _frame = frame;
        Selected = selected;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), _background);
        Vector2 origin = Origin();
        DrawLine(new Vector2(0f, origin.Y), new Vector2(Size.X, origin.Y), _axis);
        DrawLine(new Vector2(origin.X, 0f), new Vector2(origin.X, Size.Y), _axis);
        if (MoveBoxes.Body(_move, _frame) is BoxArea body)
        {
            DrawRect(ToScreen(body), _body);
        }

        foreach (JsonObject box in MoveBoxes.OnFrame(_move, _frame))
        {
            if (BoxArea.Of(box) is not BoxArea area)
            {
                continue;
            }

            Rect2 rect = ToScreen(area);
            Color color = ColorOf(box);
            DrawRect(rect, color with { A = FillAlpha });
            DrawRect(rect, color, filled: false, width: box == Selected ? SelectedWidth : OutlineWidth);
        }

        if (SelectedArea() is BoxArea selected)
        {
            foreach (Vector2 corner in Corners(ToScreen(selected)))
            {
                DrawRect(new Rect2(corner - (Vector2.One * HandleSize / 2f), Vector2.One * HandleSize), _handle);
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel:
                Zoom(wheel.Position, wheel.ButtonIndex == MouseButton.WheelUp ? ZoomStep : 1f / ZoomStep);
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right or MouseButton.Middle } pan:
                _panning = pan.Pressed;
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                if (click.Pressed)
                {
                    StartDrag(click.Position);
                }
                else
                {
                    EndDrag(click.Position, click.AltPressed);
                }

                AcceptEvent();
                break;
            case InputEventMouseMotion motion:
                Drag(motion);
                break;
        }
    }

    private void StartDrag(Vector2 position)
    {
        _dragStart = ToData(position);
        _moved = false;
        _drawing = false;
        _handleDragged = HandleAt(position);
        if (_handleDragged == BoxHandle.None)
        {
            JsonObject? box = BoxAt(_dragStart);
            if (box is not null)
            {
                Select(box);
                _handleDragged = BoxHandle.Move;
            }
            else
            {
                Select(null);
                _drawing = DrawKind is not null;
            }
        }

        _startArea = SelectedArea() ?? default;
    }

    private void Drag(InputEventMouseMotion motion)
    {
        if (_panning)
        {
            _origin = Origin() + motion.Relative;
            QueueRedraw();
            return;
        }

        if ((motion.ButtonMask & MouseButtonMask.Left) == 0 || (_handleDragged == BoxHandle.None && !_drawing))
        {
            return;
        }

        if (!_moved && _handleDragged != BoxHandle.None && ThisFrameOnly)
        {
            // Split only once the box really moves, so a click to select it leaves the file alone.
            Select(MoveBoxes.Isolate(_move, Selected!, _frame));
        }

        _moved = true;
        if (_handleDragged != BoxHandle.None)
        {
            Vector2I offset = ToData(motion.Position) - _dragStart;
            _startArea.Dragged(_handleDragged, offset.X, offset.Y, motion.AltPressed ? 1 : PixelSnap).WriteTo(Selected!);
        }

        QueueRedraw();
    }

    private void EndDrag(Vector2 position, bool noSnap)
    {
        bool drew = _drawing && _moved;
        if (drew)
        {
            Vector2I end = ToData(position);
            BoxArea area = BoxArea.Between(_dragStart.X, _dragStart.Y, end.X, end.Y, noSnap ? 1 : PixelSnap);
            Select(MoveBoxes.Add(_move, DrawKind!, _frame, area, _fieldOrder));
        }

        if (_moved)
        {
            Edited?.Invoke();
        }

        _handleDragged = BoxHandle.None;
        _drawing = false;
        _moved = false;
        QueueRedraw();
    }

    private void Select(JsonObject? box)
    {
        if (box != Selected)
        {
            Selected = box;
            SelectionChanged?.Invoke(box);
        }

        QueueRedraw();
    }

    // The top-most box under the point: the last one drawn.
    private JsonObject? BoxAt(Vector2I point)
    {
        IReadOnlyList<JsonObject> boxes = MoveBoxes.OnFrame(_move, _frame);
        for (int i = boxes.Count - 1; i >= 0; i--)
        {
            if (BoxArea.Of(boxes[i]) is BoxArea area && area.Contains(point.X, point.Y))
            {
                return boxes[i];
            }
        }

        return null;
    }

    // A corner or edge of the selected box within reach of the pointer; the box's inside moves it.
    private BoxHandle HandleAt(Vector2 position)
    {
        if (SelectedArea() is not BoxArea area)
        {
            return BoxHandle.None;
        }

        Rect2 rect = ToScreen(area);
        bool left = Mathf.Abs(position.X - rect.Position.X) <= HandleSize;
        bool right = Mathf.Abs(position.X - rect.End.X) <= HandleSize;
        bool top = Mathf.Abs(position.Y - rect.Position.Y) <= HandleSize;
        bool bottom = Mathf.Abs(position.Y - rect.End.Y) <= HandleSize;
        bool withinX = position.X >= rect.Position.X - HandleSize && position.X <= rect.End.X + HandleSize;
        bool withinY = position.Y >= rect.Position.Y - HandleSize && position.Y <= rect.End.Y + HandleSize;
        return (left, right, top, bottom) switch
        {
            (true, _, true, _) => BoxHandle.TopLeft,
            (_, true, true, _) => BoxHandle.TopRight,
            (true, _, _, true) => BoxHandle.BottomLeft,
            (_, true, _, true) => BoxHandle.BottomRight,
            (true, _, _, _) when withinY => BoxHandle.Left,
            (_, true, _, _) when withinY => BoxHandle.Right,
            (_, _, true, _) when withinX => BoxHandle.Top,
            (_, _, _, true) when withinX => BoxHandle.Bottom,
            _ => BoxHandle.None,
        };
    }

    private BoxArea? SelectedArea() =>
        Selected is not null && MoveBoxes.Covers(Selected, _frame) ? BoxArea.Of(Selected) : null;

    private void Zoom(Vector2 around, float factor)
    {
        float zoom = Mathf.Clamp(_zoom * factor, MinZoom, MaxZoom);
        // Keep the point under the pointer where it is.
        _origin = around + ((Origin() - around) * (zoom / _zoom));
        _zoom = zoom;
        QueueRedraw();
    }

    private Vector2 Origin() => _origin ?? new Vector2(Size.X / 2f, Size.Y * FloorFromTop);

    // Data y points up, screen y down; a box's top (Y + Height) is its rectangle's top edge.
    private Rect2 ToScreen(BoxArea area) => new(
        Origin() + (new Vector2(area.X, -area.Top) / Subpixels * _zoom),
        new Vector2(area.Width, area.Height) / Subpixels * _zoom);

    private Vector2I ToData(Vector2 position)
    {
        Vector2 data = (position - Origin()) / _zoom * Subpixels;
        return new Vector2I(Mathf.RoundToInt(data.X), Mathf.RoundToInt(-data.Y));
    }

    private static Vector2[] Corners(Rect2 rect) =>
        [rect.Position, new Vector2(rect.End.X, rect.Position.Y), new Vector2(rect.Position.X, rect.End.Y), rect.End];

    private static Color ColorOf(JsonObject box) =>
        JsonNodeValue.Text(box[MoveFields.Kind]) is string name && CharacterJsonNames.BoxKinds.TryGetValue(name, out BoxKind kind)
            ? BoxOverlayArt.ColorOf(kind)
            : _unknownKind;
}
#endif
