using FightingGame.Presentation.Feel;
using Godot;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>Shakes the view on heavy contact; a shake whose event a rollback removes stops at once.</summary>
/// <remarks>
/// The offset is a function of each shake's age in logic frames (ARCHITECTURE.md §9): it flips direction every frame
/// and shrinks to nothing over <see cref="ShakeSettings.LifetimeFrames"/>. Overlapping shakes don't add up; the
/// strongest wins.
/// </remarks>
public sealed class ScreenShake : IFrameEventEffect
{
    // More shakes than can overlap in a busy exchange.
    private const int MaxShakes = 8;

    private readonly LiveEvents _shakes = new(MaxShakes);
    private readonly ShakeSettings _settings;

    /// <param name="settings">Strengths and length (developer settings, presentation).</param>
    public ScreenShake(ShakeSettings settings)
    {
        _settings = settings;
    }

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
        int lifetime = _settings.LifetimeFrames;
        _shakes.Expire(latestFrame, lifetime);
        float strongest = 0f;
        int direction = 1;
        foreach (FrameEvent shake in _shakes.All)
        {
            int age = latestFrame - shake.Frame;
            float strength = StrengthOf(shake.Kind) * (lifetime - age) / lifetime;
            if (strength > strongest)
            {
                strongest = strength;
                direction = age % 2 == 0 ? 1 : -1;
            }
        }

        return new Vector2(strongest * direction, -strongest * direction * _settings.VerticalShare);
    }

    private float StrengthOf(FrameEventKind kind) => kind switch
    {
        FrameEventKind.Hit or FrameEventKind.ProjectileHit => _settings.HitPixels,
        FrameEventKind.CounterHit or FrameEventKind.PunishCounter => _settings.CounterPixels,
        FrameEventKind.ThrowHit => _settings.ThrowPixels,
        FrameEventKind.KnockOut => _settings.KnockOutPixels,
        _ => 0f,
    };
}
