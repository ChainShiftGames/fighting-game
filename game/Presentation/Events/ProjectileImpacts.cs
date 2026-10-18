using System;
using FightingGame.Presentation.World.Projectiles;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>Which projectile impacts are playing: one per hit, block, clash or expiry, gone when it ends or rollback removes it.</summary>
/// <remarks>The impact views draw them; this part has no engine calls so it can be tested.</remarks>
public sealed class ProjectileImpacts : IFrameEventEffect
{
    /// <summary>How long an impact plays, in logic frames (developer setting, presentation).</summary>
    public const int LifetimeFrames = 30;

    // Developer setting (presentation): more impacts than a busy exchange can have on screen at once.
    private const int MaxImpacts = 8;

    private readonly LiveEvents _impacts = new(MaxImpacts);

    public void Play(in FrameEvent frameEvent)
    {
        if (KindOf(frameEvent.Kind) is not null)
        {
            _impacts.Add(in frameEvent);
        }
    }

    public void Cancel(in FrameEvent frameEvent) => _impacts.Remove(frameEvent.Id);

    /// <summary>The impacts to draw at <paramref name="latestFrame"/>, oldest first.</summary>
    /// <param name="latestFrame">The newest simulated frame, the one on screen (<c>MatchState.Frame</c> − 1).</param>
    public ReadOnlySpan<FrameEvent> Live(int latestFrame)
    {
        _impacts.Expire(latestFrame, LifetimeFrames);
        return _impacts.All;
    }

    /// <summary>The impact a projectile event shows, or null for an event that ends no projectile.</summary>
    public static ImpactKind? KindOf(FrameEventKind kind) => kind switch
    {
        FrameEventKind.ProjectileHit => ImpactKind.Hit,
        FrameEventKind.ProjectileBlock => ImpactKind.Block,
        FrameEventKind.ProjectileClash => ImpactKind.Clash,
        FrameEventKind.ProjectileExpire => ImpactKind.Fizzle,
        _ => null,
    };
}
