#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>
/// The strip under the map: the selected move frame by frame, one cell per frame in its phase colour, a white bar
/// on frames that can hit, a mark where a projectile appears, and the frame the preview shows outlined. Click a
/// cell to show that frame.
/// </summary>
[Tool]
public partial class FrameStrip : Control
{
    private const float PadX = 18f;
    private const float PadTop = 14f;
    private const float HeaderHeight = 18f;
    private const float CellTop = 44f;
    private const float CellHeight = 34f;
    private const float CellGap = 2f;
    private const float NumberGap = 13f;
    private const float StripHeight = 132f;
    private const float MinCellForEveryNumber = 15f;
    private const int NumberEvery = 5;

    private string _title = "";
    private FramePhases? _phases;
    private IReadOnlySet<int> _hitFrames = new HashSet<int>();
    private HashSet<int> _spawns = [];
    private int _frame;

    public FrameStrip()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    /// <summary>A frame's cell was clicked.</summary>
    public event Action<int>? FrameClicked;

    public override void _Ready() => CustomMinimumSize = new Vector2(0f, MapLook.Of(this).Px(StripHeight));

    /// <param name="name">The move's plain name; null when no move is selected.</param>
    public void ShowMove(string? name, System.Text.Json.Nodes.JsonObject? move)
    {
        _phases = move is null ? null : FramePhases.Of(move);
        _title = name is null ? "No move selected"
            : _phases is null ? $"{name} · frames unreadable"
            : string.Create(CultureInfo.InvariantCulture, $"{name} · {_phases.TotalFrames} frames");
        _hitFrames = move is null ? new HashSet<int>() : MoveFacts.Of(move).HitFrames;
        _spawns = [];
        foreach (System.Text.Json.Nodes.JsonObject projectile in move is null ? [] : ProjectileFields.All(move))
        {
            if (ProjectileFields.SpawnFrame(projectile) is int spawn)
            {
                _spawns.Add(spawn);
            }
        }

        QueueRedraw();
    }

    public void MarkFrame(int frame)
    {
        _frame = frame;
        QueueRedraw();
    }

    public override void _Draw()
    {
        MapLook look = MapLook.Of(this);
        DrawRect(new Rect2(Vector2.Zero, Size), MapPalette.Panel);
        DrawRect(new Rect2(0f, 0f, Size.X, Mathf.Max(1f, look.Px(1f))), MapPalette.Line);
        float x = look.Px(PadX);
        float headerBaseline = look.Px(PadTop + HeaderHeight - 4f);
        int size = look.FontSize(13f);
        DrawString(look.Bold, new Vector2(x, headerBaseline), _title, HorizontalAlignment.Left, -1f, size, MapPalette.Text);
        float titleWidth = look.Bold.GetStringSize(_title, HorizontalAlignment.Left, -1f, size).X;
        DrawString(look.Body, new Vector2(x + titleWidth + look.Px(12f), headerBaseline),
            "Click a frame to see it. The white bar marks frames that can hit; ◆ where a projectile appears.",
            HorizontalAlignment.Left, Size.X - x - titleWidth - look.Px(PadX + 12f), size, MapPalette.Muted);
        if (_phases is null)
        {
            return;
        }

        int frames = _phases.TotalFrames;
        float cell = CellWidth(look, frames);
        bool everyNumber = cell >= look.Px(MinCellForEveryNumber);
        for (int frame = 1; frame <= frames; frame++)
        {
            Rect2 rect = Cell(look, frame, cell);
            Color color = Phase(frame);
            ShapeArt.Panel(this, rect, frame == _frame ? color : color with { A = 0.8f }, look.Px(4f));
            if (_hitFrames.Contains(frame))
            {
                var bar = new Rect2(rect.Position.X + look.Px(3f), rect.End.Y - look.Px(7f),
                    rect.Size.X - look.Px(6f), look.Px(4f));
                ShapeArt.Panel(this, bar, Colors.White, look.Px(2f));
            }

            if (_spawns.Contains(frame))
            {
                DrawString(look.Mono, new Vector2(rect.Position.X, rect.Position.Y + look.Px(14f)), "◆",
                    HorizontalAlignment.Center, rect.Size.X, look.FontSize(11f), Colors.White);
            }

            if (frame == _frame)
            {
                DrawRect(rect.Grow(look.Px(1.5f)), Colors.White, filled: false, width: look.Px(2f));
            }

            if (everyNumber || frame == 1 || frame % NumberEvery == 0)
            {
                DrawString(look.Mono, new Vector2(rect.Position.X - look.Px(8f), rect.End.Y + look.Px(NumberGap)),
                    frame.ToString(CultureInfo.InvariantCulture), HorizontalAlignment.Center,
                    rect.Size.X + look.Px(16f), look.FontSize(10f), MapPalette.Muted);
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click && _phases is not null)
        {
            MapLook look = MapLook.Of(this);
            float cell = CellWidth(look, _phases.TotalFrames);
            int frame = (int)((click.Position.X - look.Px(PadX)) / (cell + look.Px(CellGap))) + 1;
            if (frame >= 1 && frame <= _phases.TotalFrames && click.Position.Y >= look.Px(CellTop))
            {
                FrameClicked?.Invoke(frame);
                AcceptEvent();
            }
        }
    }

    private float CellWidth(MapLook look, int frames)
    {
        float usable = Size.X - (2f * look.Px(PadX)) - (look.Px(CellGap) * (frames - 1));
        return Mathf.Max(2f, usable / Mathf.Max(1, frames));
    }

    private Rect2 Cell(MapLook look, int frame, float cell) => new(
        look.Px(PadX) + ((frame - 1) * (cell + look.Px(CellGap))), look.Px(CellTop), cell, look.Px(CellHeight));

    private Color Phase(int frame) => _phases switch
    {
        { IsBasic: true } => MapPalette.Loop,
        { } phases when frame < phases.FirstActiveFrame => MapPalette.Windup,
        { } phases when frame <= phases.LastActiveFrame => MapPalette.Active,
        _ => MapPalette.Recovery,
    };
}
#endif
