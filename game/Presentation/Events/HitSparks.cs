using System;
using Sim.Events;
using Sim.State;

namespace FightingGame.Presentation.Events;

/// <summary>Which hit sparks are on screen: one per contact event, gone when it runs out or rollback removes it.</summary>
/// <remarks>
/// A counter hit doesn't get a spark of its own: it marks its hit's spark, which then draws in the counter look.
/// The spark views draw them; this part has no engine calls so it can be tested.
/// </remarks>
public sealed class HitSparks : IFrameEventEffect
{
    /// <summary>
    /// How long a spark is kept, in logic frames (developer setting, presentation): at least as long as the longest
    /// spark plays (hit_spark.gdshader), which ends on its own before this.
    /// </summary>
    public const int LifetimeFrames = 24;

    /// <summary>More sparks than can be on screen at once in a busy exchange (developer setting, presentation).</summary>
    public const int MaxSparks = 16;

    private readonly LiveSpark[] _sparks = new LiveSpark[MaxSparks];
    private int _count;

    public void Play(in FrameEvent frameEvent)
    {
        if (HasSpark(frameEvent.Kind))
        {
            Add(in frameEvent);
        }
        else if (IsCounter(frameEvent.Kind) && FindHitOf(in frameEvent) is int hit)
        {
            _sparks[hit].Counter = frameEvent.Kind;
        }
    }

    public void Cancel(in FrameEvent frameEvent)
    {
        if (IsCounter(frameEvent.Kind))
        {
            if (FindHitOf(in frameEvent) is int hit && _sparks[hit].Counter == frameEvent.Kind)
            {
                _sparks[hit].Counter = FrameEventKind.None;
            }

            return;
        }

        for (int i = 0; i < _count; i++)
        {
            if (_sparks[i].Event.Id == frameEvent.Id)
            {
                RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>The sparks to draw at <paramref name="latestFrame"/>, oldest first; call once per displayed frame.</summary>
    /// <remarks>
    /// A spark seen for the first time shows its first drawing (age 0), even when a rollback found its hit a few
    /// frames late: the opening flash is what sells the hit. After that it shows the age the logic frame gives.
    /// </remarks>
    /// <param name="latestFrame">The newest simulated frame, the one on screen (<c>MatchState.Frame</c> − 1).</param>
    /// <param name="fighters">The fighters now, for the attacker's facing on a spark's first showing.</param>
    public ReadOnlySpan<LiveSpark> Live(int latestFrame, in FighterStates fighters)
    {
        Expire(latestFrame);
        for (int i = 0; i < _count; i++)
        {
            ref LiveSpark spark = ref _sparks[i];
            if (spark.Facing == 0)
            {
                spark.Facing = FacingOf(spark.Event.Fighter, in fighters);
                spark.ShownAge = 0;
            }
            else
            {
                spark.ShownAge = latestFrame - spark.Event.Frame;
            }
        }

        return _sparks.AsSpan(0, _count);
    }

    private static int FacingOf(int fighter, in FighterStates fighters) =>
        fighter is >= 0 and < MatchState.PlayerCount && fighters[fighter].Facing < 0 ? -1 : 1;

    private void Add(in FrameEvent frameEvent)
    {
        if (_count == _sparks.Length)
        {
            RemoveAt(0);
        }

        _sparks[_count++] = new LiveSpark(in frameEvent);
    }

    // The hit a counter event belongs to: emitted in the same frame by the same attacker at the same point.
    private int? FindHitOf(in FrameEvent counter)
    {
        for (int i = _count - 1; i >= 0; i--)
        {
            FrameEvent hit = _sparks[i].Event;
            if (hit.Kind is FrameEventKind.Hit or FrameEventKind.ProjectileHit && hit.Frame == counter.Frame &&
                hit.Fighter == counter.Fighter && hit.Position == counter.Position)
            {
                return i;
            }
        }

        return null;
    }

    private void Expire(int latestFrame)
    {
        int kept = 0;
        for (int i = 0; i < _count; i++)
        {
            if (latestFrame - _sparks[i].Event.Frame < LifetimeFrames)
            {
                _sparks[kept++] = _sparks[i];
            }
        }

        _count = kept;
    }

    private void RemoveAt(int index)
    {
        _sparks.AsSpan(index + 1, _count - index - 1).CopyTo(_sparks.AsSpan(index));
        _count--;
    }

    private static bool HasSpark(FrameEventKind kind) => kind is FrameEventKind.Hit or FrameEventKind.Block
        or FrameEventKind.Clash or FrameEventKind.ProjectileHit or FrameEventKind.ProjectileBlock
        or FrameEventKind.ProjectileClash or FrameEventKind.ThrowHit or FrameEventKind.ThrowTech;

    private static bool IsCounter(FrameEventKind kind) =>
        kind is FrameEventKind.CounterHit or FrameEventKind.PunishCounter;
}
