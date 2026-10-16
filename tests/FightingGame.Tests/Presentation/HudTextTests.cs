using FightingGame.Presentation;
using Xunit;

namespace FightingGame.Tests.Presentation;

public sealed class HudTextTests
{
    [Theory]
    [InlineData(10_000, 10_000, 1f)]
    [InlineData(2_500, 10_000, 0.25f)]
    [InlineData(-5, 10_000, 0f)]
    [InlineData(12_000, 10_000, 1f)]
    [InlineData(100, 0, 0f)]
    public void FillsTheHealthBarByShareOfMaxHealth(int health, int maxHealth, float fraction)
    {
        Assert.Equal(fraction, HudText.HealthFraction(health, maxHealth));
    }

    [Theory]
    [InlineData(5940, "99")]
    [InlineData(61, "2")]
    [InlineData(60, "1")]
    [InlineData(1, "1")]
    [InlineData(0, "∞")]
    public void ShowsWholeSecondsRoundedUp(int framesLeft, string text)
    {
        Assert.Equal(text, HudText.Timer(framesLeft, 60));
    }

    [Fact]
    public void NamesTheRound()
    {
        Assert.Equal("ROUND 1", HudText.Round(1));
    }
}
