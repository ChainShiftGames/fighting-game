#if TOOLS
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Presentation;
using Godot;
using Sim.Characters;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The move's projectiles on the <see cref="BoxCanvas"/>: their hitboxes where they are on the frame (dashed before
/// they spawn, where they will appear) and a cross on each spawn point (<c>offset</c>), and which one is under the
/// pointer, for dragging its offset.
/// </summary>
public sealed class ProjectileOverlay
{
    // 1× screen pixels: the cross's arm and how near the pointer must be to grab it.
    private const float CrossArm = 7f;
    private const float GrabReach = 9f;
    private const float FillAlpha = 0.2f;
    private const float DashLength = 5f;

    private static readonly Color _hit = BoxOverlayArt.ColorOf(BoxKind.Hit);
    private static readonly Color _cross = Look.MapPalette.Glow;

    private IReadOnlyList<ProjectileOnFrame> _shown = [];

    public void Show(JsonObject move, int frame) => _shown = ProjectilePreview.Of(move, frame);

    /// <param name="toScreen">Where a data area is on the canvas.</param>
    /// <param name="pointToScreen">Where a data point (subpixels, y up) is on the canvas.</param>
    public void Draw(Control canvas, Func<BoxArea, Rect2> toScreen, Func<int, int, Vector2> pointToScreen, JsonObject? dragged)
    {
        foreach (ProjectileOnFrame projectile in _shown)
        {
            foreach (BoxArea box in projectile.Boxes)
            {
                Rect2 rect = toScreen(box);
                if (projectile.HasSpawned)
                {
                    canvas.DrawRect(rect, _hit with { A = FillAlpha });
                    canvas.DrawRect(rect, _hit, filled: false, width: projectile.Projectile == dragged ? 3f : 1.5f);
                }
                else
                {
                    Look.ShapeArt.Dashed(canvas, rect, 0f, _hit with { A = 0.6f }, 1.5f, DashLength);
                }
            }

            if (ProjectileFields.OffsetOf(projectile.Projectile) is var (x, y))
            {
                Vector2 at = pointToScreen(x, y);
                canvas.DrawLine(at - new Vector2(CrossArm, 0f), at + new Vector2(CrossArm, 0f), _cross, 1.5f);
                canvas.DrawLine(at - new Vector2(0f, CrossArm), at + new Vector2(0f, CrossArm), _cross, 1.5f);
            }
        }
    }

    /// <summary>The projectile whose spawn cross or hitbox is under <paramref name="position"/>; spawn crosses first.</summary>
    public JsonObject? At(Vector2 position, Func<BoxArea, Rect2> toScreen, Func<int, int, Vector2> pointToScreen)
    {
        foreach (ProjectileOnFrame projectile in _shown)
        {
            if (ProjectileFields.OffsetOf(projectile.Projectile) is var (x, y) && pointToScreen(x, y).DistanceTo(position) <= GrabReach)
            {
                return projectile.Projectile;
            }
        }

        foreach (ProjectileOnFrame projectile in _shown)
        {
            foreach (BoxArea box in projectile.Boxes)
            {
                if (toScreen(box).HasPoint(position))
                {
                    return projectile.Projectile;
                }
            }
        }

        return null;
    }
}
#endif
