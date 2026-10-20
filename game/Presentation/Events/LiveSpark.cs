using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>A hit spark on screen: its contact event, plus what presentation learned about it after it played.</summary>
public struct LiveSpark
{
    public LiveSpark(in FrameEvent contact)
    {
        Event = contact;
    }

    /// <summary>The contact event (hit, block, clash, throw…) the spark is drawn for.</summary>
    public FrameEvent Event { get; }

    /// <summary>
    /// <see cref="FrameEventKind.CounterHit"/> or <see cref="FrameEventKind.PunishCounter"/> when the hit was one;
    /// <see cref="FrameEventKind.None"/> otherwise.
    /// </summary>
    public FrameEventKind Counter { get; internal set; }

    /// <summary>The attacker's facing the first time the spark was shown (1 right, -1 left); 0 before that.</summary>
    /// <remarks>Kept, so a side switch while the spark plays doesn't flip it.</remarks>
    public int Facing { get; internal set; }

    /// <summary>The spark's age in logic frames to draw this displayed frame (see <see cref="HitSparks.Live"/>).</summary>
    public int ShownAge { get; internal set; }
}
