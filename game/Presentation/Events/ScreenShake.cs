using Godot;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>Shakes the view on heavy contact; a shake whose event a rollback removes stops at once.</summary>
/// <remarks>
/// The offset is a function of each shake's age in logic frames (ARCHITECTURE.md §9): it flips direction every frame
/// and shrinks to nothing over <see cref="LifetimeFrames"/>. Overlapping shakes don't add up; the strongest wins.
/// </remarks>
public sealed class ScreenShake : IFrameEventEffect
{
    /// <summary>How long a shake lasts, in logic frames (developer setting, presentation).</summary>
    public const int LifetimeFrames = 8;

    // Developer settings (presentation): starting strength in screen pixels per kind, and how many shakes are kept.
    private const float HitPixels = 3f;
    private const float CounterPixels = 5f;
    private const float ThrowPixels = 4f;
    private const float KnockOutPixels = 8f;
    private const int MaxShakes = 8;

    // Developer setting (presentation): vertical movement as a share of the horizontal one.
    private const float VerticalShare = 0.5f;

    private readonly LiveEvents _shakes = new(MaxShakes);

    public void Play(in FrameEvent frameEvent)
    {
        if (StrengthOf(frameEvent.Kind) > 0f)
        {
            _shakes.Add(in frameEvent);
        }
    }

    public void Cancel(in FrameEvent frameEvent) => _shakes.Remove(frameEvent.Id);

    /// <summary>How far to move the view at <paramref name="latestFrame"/>, in screen pixels.</summary>
    /// <param name="latestFrame">The newest simulated frame, the one on screen (<c>MatchState.Frame</c> − 1).</param>
    public Vector2 Offset(int latestFrame)
    {
        _shakes.Expire(latestFrame, LifetimeFrames);
        float strongest = 0f;
        int direction = 1;
        foreach (FrameEvent shake in _shakes.All)
        {
            int age = latestFrame - shake.Frame;
            float strength = StrengthOf(shake.Kind) * (LifetimeFrames - age) / LifetimeFrames;
            if (strength > strongest)
            {
                strongest = strength;
                direction = age % 2 == 0 ? 1 : -1;
            }
        }

        return new Vector2(strongest * direction, -strongest * direction * VerticalShare);
    }

    private static float StrengthOf(FrameEventKind kind) => kind switch
    {
        FrameEventKind.Hit or FrameEventKind.ProjectileHit => HitPixels,
        FrameEventKind.CounterHit or FrameEventKind.PunishCounter => CounterPixels,
        FrameEventKind.ThrowHit => ThrowPixels,
        FrameEventKind.KnockOut => KnockOutPixels,
        _ => 0f,
    };
}
