using FightingGame.Presentation.Hud;
using Godot;
using Xunit;

namespace FightingGame.Tests.Presentation.Hud;

public sealed class HudLayoutTests
{
    [Fact]
    public void MirrorsP2AcrossTheScreenCentre()
    {
        var layout = HudLayout.For(new Vector2(1920f, 1080f));

        Rect2 p1 = layout.HealthBar(0);
        Rect2 p2 = layout.HealthBar(1);

        Assert.Equal(1920f - p1.End.X, p2.Position.X, 3);
        Assert.Equal(p1.Size, p2.Size);
        Assert.Equal(1920f - layout.MeterSegment(0, 0).End.X, layout.MeterSegment(1, 0).Position.X, 3);
    }

    [Fact]
    public void KeepsTheTimerCentredBetweenTheBars()
    {
        var layout = HudLayout.For(new Vector2(1280f, 720f));

        Assert.Equal(640f, layout.Timer.GetCenter().X, 3);
        Assert.True(layout.HealthBar(0).End.X < layout.Timer.Position.X);
        Assert.True(layout.HealthBar(1).Position.X > layout.Timer.End.X);
    }

    [Fact]
    public void KeepsSixteenByNineOnAWiderScreen()
    {
        var layout = HudLayout.For(new Vector2(2560f, 1080f));

        Assert.Equal(1.5f, layout.Scale, 3);
        Assert.Equal(1280f, layout.Timer.GetCenter().X, 3);
        Assert.True(layout.HealthBar(0).Position.X > 320f);
    }

    [Fact]
    public void FillsTheOuterMeterSegmentFirst()
    {
        var layout = HudLayout.For(new Vector2(1280f, 720f));

        Assert.True(layout.MeterSegment(0, 0).Position.X < layout.MeterSegment(0, 1).Position.X);
        Assert.True(layout.MeterSegment(1, 0).Position.X > layout.MeterSegment(1, 1).Position.X);
    }
}
