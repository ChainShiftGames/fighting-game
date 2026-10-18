using FightingGame.Presentation.Events;
using FightingGame.Presentation.World.Projectiles;
using Sim.Events;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class ProjectileImpactsTests
{
    private readonly ProjectileImpacts _impacts = new();

    [Theory]
    [InlineData(FrameEventKind.ProjectileHit, ImpactKind.Hit)]
    [InlineData(FrameEventKind.ProjectileBlock, ImpactKind.Block)]
    [InlineData(FrameEventKind.ProjectileClash, ImpactKind.Clash)]
    [InlineData(FrameEventKind.ProjectileExpire, ImpactKind.Fizzle)]
    public void PlaysAnImpactForEveryWayAProjectileEnds(FrameEventKind kind, ImpactKind impact)
    {
        _impacts.Play(Event(10, kind));

        Assert.Equal(1, _impacts.Live(10).Length);
        Assert.Equal(impact, ProjectileImpacts.KindOf(kind));
    }

    [Theory]
    [InlineData(FrameEventKind.Hit)]
    [InlineData(FrameEventKind.ProjectileSpawn)]
    public void IgnoresEventsThatEndNoProjectile(FrameEventKind kind)
    {
        _impacts.Play(Event(10, kind));

        Assert.Equal(0, _impacts.Live(10).Length);
    }

    [Fact]
    public void EndsAfterItsLifetime()
    {
        _impacts.Play(Event(10, FrameEventKind.ProjectileHit));

        Assert.Equal(1, _impacts.Live(10 + ProjectileImpacts.LifetimeFrames - 1).Length);
        Assert.Equal(0, _impacts.Live(10 + ProjectileImpacts.LifetimeFrames).Length);
    }

    [Fact]
    public void StopsWhenARollbackRemovesTheHit()
    {
        FrameEvent hit = Event(10, FrameEventKind.ProjectileHit);
        _impacts.Play(hit);

        _impacts.Cancel(hit);

        Assert.Equal(0, _impacts.Live(11).Length);
    }

    private static FrameEvent Event(int frame, FrameEventKind kind) =>
        new(frame, kind, fighter: 0, occurrence: 0, new IntVector2(100_000, 60_000), detail: 0);
}
