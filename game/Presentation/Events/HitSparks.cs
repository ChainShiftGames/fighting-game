using System;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>Which hit sparks are on screen: one per contact event, gone when it runs out or rollback removes it.</summary>
/// <remarks><see cref="HitSparkArt"/> draws them; this part has no engine calls so it can be tested.</remarks>
public sealed class HitSparks : IFrameEventEffect
{
    /// <summary>How long a spark shows, in logic frames (developer setting, presentation).</summary>
    public const int LifetimeFrames = 12;

    // Developer setting (presentation): more sparks than can be on screen at once in a busy exchange.
    private const int MaxSparks = 16;

    private readonly LiveEvents _sparks = new(MaxSparks);

    public void Play(in FrameEvent frameEvent)
    {
        if (HasSpark(frameEvent.Kind))
        {
            _sparks.Add(in frameEvent);
        }
    }

    public void Cancel(in FrameEvent frameEvent) => _sparks.Remove(frameEvent.Id);

    /// <summary>The sparks to draw at <paramref name="latestFrame"/>, oldest first.</summary>
    /// <param name="latestFrame">The newest simulated frame, the one on screen (<c>MatchState.Frame</c> − 1).</param>
    public ReadOnlySpan<FrameEvent> Live(int latestFrame)
    {
        _sparks.Expire(latestFrame, LifetimeFrames);
        return _sparks.All;
    }

    private static bool HasSpark(FrameEventKind kind) => kind is FrameEventKind.Hit or FrameEventKind.Block
        or FrameEventKind.Clash or FrameEventKind.CounterHit or FrameEventKind.PunishCounter
        or FrameEventKind.ProjectileHit or FrameEventKind.ProjectileBlock or FrameEventKind.ProjectileClash
        or FrameEventKind.ThrowHit or FrameEventKind.ThrowTech;
}
