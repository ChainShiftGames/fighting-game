using System;
using Godot;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>Placeholder art for hit sparks: a burst that grows and fades over the spark's lifetime.</summary>
/// <remarks>
/// The size and fade come from the spark's age in logic frames, not elapsed time (ARCHITECTURE.md §9), so a spark
/// a rollback starts late shows the frame it would be on. Final art replaces this class with the same input.
/// </remarks>
public static class HitSparkArt
{
    // Developer settings (presentation): burst size in screen pixels at the first and last frame, ray width and
    // count, and where along the radius a ray starts.
    private const float StartRadius = 6f;
    private const float EndRadius = 22f;
    private const float RayWidth = 3f;
    private const int Rays = 8;
    private const float RayStartShare = 0.4f;

    private static readonly Color _hitColor = new(1f, 0.9f, 0.4f);
    private static readonly Color _blockColor = new(0.5f, 0.8f, 1f);
    private static readonly Color _counterColor = new(1f, 0.3f, 0.2f);
    private static readonly Color _otherColor = new(0.9f, 0.9f, 0.95f);

    /// <param name="latestFrame">The newest simulated frame, the one on screen (<c>MatchState.Frame</c> − 1).</param>
    /// <param name="drawnElsewhere">True for sparks another effect replaces (a projectile's own impact); null draws all.</param>
    public static void Draw(
        CanvasItem canvas,
        StageCamera camera,
        ReadOnlySpan<FrameEvent> sparks,
        int latestFrame,
        Func<FrameEvent, bool>? drawnElsewhere = null)
    {
        foreach (FrameEvent spark in sparks)
        {
            if (drawnElsewhere?.Invoke(spark) == true)
            {
                continue;
            }

            float progress = Math.Clamp((float)(latestFrame - spark.Frame) / HitSparks.LifetimeFrames, 0f, 1f);
            float radius = StartRadius + ((EndRadius - StartRadius) * progress);
            Color color = ColorOf(spark.Kind) with { A = 1f - progress };
            DrawBurst(canvas, camera.ToScreen(spark.Position), radius, color);
        }
    }

    private static void DrawBurst(CanvasItem canvas, Vector2 centre, float radius, Color color)
    {
        for (int ray = 0; ray < Rays; ray++)
        {
            Vector2 direction = Vector2.Right.Rotated(Mathf.Tau * ray / Rays);
            Vector2 start = centre + (direction * radius * RayStartShare);
            canvas.DrawLine(start, centre + (direction * radius), color, RayWidth);
        }
    }

    private static Color ColorOf(FrameEventKind kind) => kind switch
    {
        FrameEventKind.Hit or FrameEventKind.ProjectileHit or FrameEventKind.ThrowHit => _hitColor,
        FrameEventKind.Block or FrameEventKind.ProjectileBlock => _blockColor,
        FrameEventKind.CounterHit or FrameEventKind.PunishCounter => _counterColor,
        _ => _otherColor,
    };
}
