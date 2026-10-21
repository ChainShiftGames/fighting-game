#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Presentation;
using FightingGame.Presentation.World.Sparks;
using Godot;
using Sim.Characters;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// One frame of a move, facing right: the character's model posed for the frame (<see cref="ModelPreview"/>), or the
/// placeholder body when it has none, every box on the frame in the hitbox overlay's colours, and the selected box with handles. Left-drag moves or resizes the selected box, or draws a
/// new box of <see cref="DrawKind"/> on empty space, or a projectile's spawn point (its cross or hitbox,
/// <see cref="ProjectileOverlay"/>), or the hit spark's star (<see cref="SparkMoved"/>); right- or middle-drag pans;
/// the wheel zooms.
/// </summary>
/// <remarks>
/// Edges snap to whole pixels; hold Alt to place them on any subpixel. While not <see cref="Editable"/> a left click
/// only raises <see cref="Clicked"/> (the drawer then opens its hit areas tab); panning and zooming still work. An
/// opponent standing at point blank (<see cref="Opponent"/>) is outlined, so the reach of hit boxes shows.
/// </remarks>
[Tool]
public partial class BoxCanvas : Control
{
    private const float Subpixels = 1000f;
    private const int PixelSnap = 1000;
    private const float StartZoom = 2f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 12f;
    private const float ZoomStep = 1.15f;
    private const float FloorFromTop = 0.88f;
    private const float OriginFromLeft = 0.4f;
    // Developer setting (editor look): the height in pixels a reset view fits, a standing body with headroom.
    private const float FittedHeight = 150f;
    private const float HandleSize = 7f;
    private const float FillAlpha = 0.2f;
    private const float OutlineWidth = 1.5f;
    private const float SelectedWidth = 3f;
    // Developer settings (editor look): the spark marker's star radius and direction arrow length, in screen pixels.
    private const float SparkRadius = 9f;
    private const float SparkArrow = 28f;

    private static readonly Color _axis = new(1f, 1f, 1f, 0.07f);
    private static readonly Color _opponent = Look.MapPalette.Muted with { A = 0.75f };
    private static readonly Color _body = new(0.55f, 0.55f, 0.6f, 0.45f);
    private static readonly Color _unknownKind = new(0.6f, 0.6f, 0.6f);
    private static readonly Color _handle = new(1f, 1f, 1f);
    private static readonly Color _spark = new(1f, 0.8f, 0.22f);

    private readonly ModelPreview _preview = new();
    private readonly ProjectileOverlay _projectiles = new();
    private readonly GradientTexture2D _backdrop = new()
    {
        Gradient = new Gradient
        {
            Colors = [Look.MapPalette.PreviewCentre, Look.MapPalette.Panel],
            Offsets = [0f, 1f],
        },
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.35f),
        FillTo = new Vector2(1.05f, 0.95f),
    };
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

    // A projectile whose spawn offset is being dragged, and its offset when the drag started.
    private JsonObject? _projectileDragged;
    private (int X, int Y) _startOffset;

    // The spark marker is being dragged; its offset in pixels when the drag started.
    private bool _sparkDragged;
    private Vector2 _startSparkOffset;

    public BoxCanvas()
    {
        ClipContents = true;
        FocusMode = FocusModeEnum.Click;
        AddChild(_preview);
    }

    /// <summary>Clicked while not <see cref="Editable"/>.</summary>
    public event Action? Clicked;

    /// <summary>The selected box changed (null: none).</summary>
    public event Action<JsonObject?>? SelectionChanged;

    /// <summary>A drag changed the boxes: moved, resized, split out of a range or drawn.</summary>
    public event Action? Edited;

    /// <summary>The spark marker was dragged to a new offset: whole pixels forward and up from the contact point.</summary>
    public event Action<int, int>? SparkMoved;

    public JsonObject? Selected { get; private set; }

    /// <summary>The model drawn behind the boxes, which the animation track retimes.</summary>
    public ModelPreview Preview => _preview;

    /// <summary>The kind a drag on empty space draws; null to only select.</summary>
    public string? DrawKind { get; set; }

    /// <summary>Dragging a box that spans several frames first splits this frame out of it.</summary>
    public bool ThisFrameOnly { get; set; } = true;

    /// <summary>Left-drags select, move and draw boxes; otherwise the canvas only shows them.</summary>
    public bool Editable { get; set; } = true;

    /// <summary>Hurt boxes of an opponent at point blank, outlined behind the move's boxes; empty for none.</summary>
    public IReadOnlyList<BoxArea> Opponent { get; set; } = [];

    /// <summary>The spark settings of the hit being edited, marked where its spark starts; null for no marker.</summary>
    public HitSparkSettings? Spark { get; set; }

    /// <summary>Shows the character's model behind the boxes, when its art file has one; null for none.</summary>
    public void ShowCharacter(string? characterId, IReadOnlyList<string> moveIds)
    {
        _preview.ShowCharacter(characterId, moveIds);
        QueueRedraw();
    }

    /// <summary>Forgets panning and zooming: the body fits the canvas height again, standing on its floor.</summary>
    public void ResetView()
    {
        _origin = null;
        _zoom = Mathf.Clamp(Size.Y * FloorFromTop / FittedHeight, MinZoom, MaxZoom);
        QueueRedraw();
    }

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
        DrawTextureRect(_backdrop, new Rect2(Vector2.Zero, Size), tile: false);
        Vector2 origin = Origin();
        DrawLine(new Vector2(0f, origin.Y), new Vector2(Size.X, origin.Y), Look.MapPalette.Line, 1.5f);
        DrawLine(new Vector2(origin.X, 0f), new Vector2(origin.X, origin.Y), _axis);
        foreach (BoxArea area in Opponent)
        {
            Look.ShapeArt.Dashed(this, ToScreen(area), 6f * _zoom, _opponent, 1.5f, 5f);
        }
        if (_preview.HasModel)
        {
            _preview.ShowFrame(_move, _frame, Size, origin, _zoom / Subpixels);
            DrawTexture(_preview.GetTexture(), Vector2.Zero);
        }
        else if (MoveBoxes.Body(_move, _frame) is BoxArea body)
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

        DrawSpark();
        _projectiles.Show(_move, _frame);
        _projectiles.Draw(this, ToScreen, ToScreen, _projectileDragged);
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
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } when !Editable:
                Clicked?.Invoke();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } when !Editable:
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
        // The star sits inside a hit box, so it's picked before the box under it.
        if (_handleDragged == BoxHandle.None && Spark is { } spark && SparkCentre() is Vector2 centre
            && position.DistanceTo(centre) <= SparkRadius + HandleSize)
        {
            _sparkDragged = true;
            _startSparkOffset = new Vector2(spark.OffsetX, spark.OffsetY);
            return;
        }

        if (_handleDragged == BoxHandle.None && _projectiles.At(position, ToScreen, ToScreen) is JsonObject projectile
            && ProjectileFields.OffsetOf(projectile) is { } offset)
        {
            _projectileDragged = projectile;
            _startOffset = offset;
            return;
        }

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

        if ((motion.ButtonMask & MouseButtonMask.Left) != 0 && _projectileDragged is not null)
        {
            DragProjectile(motion);
            return;
        }

        if ((motion.ButtonMask & MouseButtonMask.Left) != 0 && _sparkDragged)
        {
            DragSpark(motion);
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
        _projectileDragged = null;
        _sparkDragged = false;
        _drawing = false;
        _moved = false;
        QueueRedraw();
    }

    // The projectile's offset follows the pointer, in whole pixels unless Alt is held, like box edges.
    private void DragProjectile(InputEventMouseMotion motion)
    {
        Vector2I moved = ToData(motion.Position) - _dragStart;
        int snap = motion.AltPressed ? 1 : PixelSnap;
        ProjectileFields.SetOffset(_projectileDragged!, Snap(_startOffset.X + moved.X, snap), Snap(_startOffset.Y + moved.Y, snap));
        _moved = true;
        QueueRedraw();
    }

    // The spark's offset follows the pointer in whole pixels, as the spark panel's steppers set it; the panel writes
    // it to the art file and hands the marker back.
    private void DragSpark(InputEventMouseMotion motion)
    {
        Vector2I moved = ToData(motion.Position) - _dragStart;
        int forward = Mathf.RoundToInt(_startSparkOffset.X + (moved.X / Subpixels));
        int up = Mathf.RoundToInt(_startSparkOffset.Y + (moved.Y / Subpixels));
        if (Spark is { } spark && (forward != (int)MathF.Round(spark.OffsetX) || up != (int)MathF.Round(spark.OffsetY)))
        {
            SparkMoved?.Invoke(forward, up);
        }
    }

    private static int Snap(int value, int snap) => (int)Math.Round(value / (double)snap, MidpointRounding.AwayFromZero) * snap;

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

    // Where the marked spark starts on this frame, on screen; null when there is no marker or no hit box.
    private Vector2? SparkCentre() =>
        Spark is { } spark && SparkContact.Of(_move, _frame, Opponent) is (int x, int y)
            ? ToScreen(x + (int)MathF.Round(spark.OffsetX * Subpixels), y + (int)MathF.Round(spark.OffsetY * Subpixels))
            : null;

    // A star where the spark starts on this frame, with an arrow along its angle when the art file sets one.
    private void DrawSpark()
    {
        if (Spark is not { } spark || SparkCentre() is not Vector2 centre)
        {
            return;
        }

        var star = new Vector2[17];
        for (int i = 0; i < star.Length; i++)
        {
            float radius = i % 2 == 0 ? SparkRadius : SparkRadius * 0.4f;
            star[i] = centre + (Vector2.FromAngle(i * MathF.PI / 8f) * radius);
        }

        DrawPolyline(star, _spark, 2f, antialiased: true);
        if (spark.Angle is float degrees)
        {
            Vector2 direction = Vector2.FromAngle(-degrees * MathF.PI / 180f);
            DrawLine(centre, centre + (direction * SparkArrow), _spark, 2f, antialiased: true);
            Look.ShapeArt.Arrowhead(this, centre + (direction * SparkArrow), direction, 7f, _spark);
        }
    }

    private Vector2 Origin() => _origin ?? new Vector2(Size.X * OriginFromLeft, Size.Y * FloorFromTop);

    // Data y points up, screen y down; a box's top (Y + Height) is its rectangle's top edge.
    private Rect2 ToScreen(BoxArea area) => new(
        Origin() + (new Vector2(area.X, -area.Top) / Subpixels * _zoom),
        new Vector2(area.Width, area.Height) / Subpixels * _zoom);

    private Vector2 ToScreen(int x, int y) => Origin() + (new Vector2(x, -y) / Subpixels * _zoom);

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
