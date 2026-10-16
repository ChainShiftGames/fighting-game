using FightingGame.Presentation.Events;
using Godot;
using Sim.Events;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class ScreenShakeTests
{
    private readonly ScreenShake _shake = new();

    [Fact]
    public void ShakesFlipEveryFrameAndFadeOut()
    {
        _shake.Play(Event(10, FrameEventKind.Hit));

        Vector2 first = _shake.Offset(10);
        Vector2 second = _shake.Offset(11);

        Assert.True(first.X > 0f);
        Assert.True(second.X < 0f);
        Assert.True(-second.X < first.X);
        Assert.Equal(Vector2.Zero, _shake.Offset(10 + ScreenShake.LifetimeFrames));
    }

    [Fact]
    public void ACancelledShakeStopsAtOnce()
    {
        FrameEvent hit = Event(10, FrameEventKind.Hit);
        _shake.Play(hit);

        _shake.Cancel(hit);

        Assert.Equal(Vector2.Zero, _shake.Offset(10));
    }

    [Fact]
    public void BlocksDoNotShake()
    {
        _shake.Play(Event(10, FrameEventKind.Block));

        Assert.Equal(Vector2.Zero, _shake.Offset(10));
    }

    private static FrameEvent Event(int frame, FrameEventKind kind) =>
        new(frame, kind, fighter: 0, occurrence: 0, IntVector2.Zero, detail: 0);
}
