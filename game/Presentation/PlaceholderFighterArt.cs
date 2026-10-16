using System.Collections.Generic;
using Godot;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;

namespace FightingGame.Presentation;

/// <summary>
/// Placeholder art for one fighter: a box body from its data, a facing marker, its attack's hitboxes as the
/// "limb" while active, and a strip showing the move's frame data phase and progress.
/// </summary>
/// <remarks>Final art replaces this class; it takes the same pose and boxes (ARCHITECTURE.md §9).</remarks>
public static class PlaceholderFighterArt
{
    private const float OutlineWidth = 2f;
    private const float StripHeight = 5f;
    private const float StripGap = 4f;
    private const float LabelGap = 8f;
    private const int LabelFontSize = 14;

    // Frame meter colours, as in most fighting games' training modes.
    private static readonly Color _startupColor = new(0.3f, 0.85f, 0.4f);
    private static readonly Color _activeColor = new(1f, 0.3f, 0.3f);
    private static readonly Color _recoveryColor = new(0.3f, 0.55f, 1f);
    private static readonly Color _noPhaseColor = new(0.5f, 0.5f, 0.55f);
    private static readonly Color _stripBackground = new(0f, 0f, 0f, 0.5f);
    private static readonly Color _labelColor = new(0.9f, 0.9f, 0.95f);

    public static void Draw(
        CanvasItem canvas, StageCamera camera, FighterPose pose, int facing, List<WorldBox> boxes, Color color)
    {
        AxisAlignedBox body = FighterBoxes.Body(boxes);
        if (body.IsEmpty)
        {
            return;
        }

        Rect2 bodyRect = camera.ToScreen(body);
        canvas.DrawRect(bodyRect, color);
        canvas.DrawRect(bodyRect, color.Darkened(0.5f), filled: false, width: OutlineWidth);
        DrawFacing(canvas, bodyRect, facing, color.Lightened(0.6f));
        if (pose.Phase == MovePhase.Active)
        {
            DrawLimbs(canvas, camera, boxes, color.Lightened(0.35f));
        }

        DrawPhaseStrip(canvas, bodyRect, pose);
        Font font = ThemeDB.FallbackFont;
        var labelPosition = new Vector2(bodyRect.Position.X - bodyRect.Size.X, bodyRect.Position.Y - LabelGap);
        canvas.DrawString(
            font, labelPosition, pose.Label, HorizontalAlignment.Center, bodyRect.Size.X * 3f, LabelFontSize, _labelColor);
    }

    // A triangle at chest height pointing the way the fighter faces.
    private static void DrawFacing(CanvasItem canvas, Rect2 body, int facing, Color color)
    {
        float size = body.Size.X * 0.3f;
        float edge = facing > 0 ? body.End.X : body.Position.X;
        float y = body.Position.Y + (body.Size.Y * 0.25f);
        canvas.DrawColoredPolygon(
            [new Vector2(edge, y - size), new Vector2(edge + (facing * size), y), new Vector2(edge, y + size)], color);
    }

    private static void DrawLimbs(CanvasItem canvas, StageCamera camera, List<WorldBox> boxes, Color color)
    {
        foreach (WorldBox box in boxes)
        {
            if (box.Kind == BoxKind.Hit)
            {
                canvas.DrawRect(camera.ToScreen(box.Area), color);
            }
        }
    }

    // Phase colour, filled up to the current frame of the move.
    private static void DrawPhaseStrip(CanvasItem canvas, Rect2 body, FighterPose pose)
    {
        var strip = new Rect2(body.Position.X, body.End.Y + StripGap, body.Size.X, StripHeight);
        canvas.DrawRect(strip, _stripBackground);
        float progress = pose.TotalFrames <= 0 ? 1f : Mathf.Clamp((float)pose.Frame / pose.TotalFrames, 0f, 1f);
        canvas.DrawRect(new Rect2(strip.Position, new Vector2(strip.Size.X * progress, StripHeight)), PhaseColor(pose.Phase));
    }

    private static Color PhaseColor(MovePhase phase) => phase switch
    {
        MovePhase.Startup => _startupColor,
        MovePhase.Active => _activeColor,
        MovePhase.Recovery => _recoveryColor,
        _ => _noPhaseColor,
    };
}
