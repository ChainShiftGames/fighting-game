using System.Collections.Generic;
using Godot;
using Sim.Characters;

namespace FightingGame.Presentation;

/// <summary>The hitbox overlay (developer setting): every box of the current frame, coloured by kind.</summary>
public static class BoxOverlayArt
{
    private const float OutlineWidth = 1.5f;
    private const float FillAlpha = 0.2f;

    private static readonly Color _hurtColor = new(0.2f, 0.9f, 0.3f);
    private static readonly Color _hitColor = new(1f, 0.15f, 0.15f);
    private static readonly Color _pushColor = new(1f, 0.85f, 0.1f);
    private static readonly Color _throwColor = new(0.3f, 0.6f, 1f);

    public static void Draw(CanvasItem canvas, StageCamera camera, List<WorldBox> boxes)
    {
        foreach (WorldBox box in boxes)
        {
            Rect2 rect = camera.ToScreen(box.Area);
            Color color = ColorOf(box.Kind);
            canvas.DrawRect(rect, color with { A = FillAlpha });
            canvas.DrawRect(rect, color, filled: false, width: OutlineWidth);
        }
    }

    /// <summary>The colour of a box kind, also used by the character editor's box canvas.</summary>
    public static Color ColorOf(BoxKind kind) => kind switch
    {
        BoxKind.Hurt => _hurtColor,
        BoxKind.Hit => _hitColor,
        BoxKind.Push => _pushColor,
        _ => _throwColor,
    };
}
