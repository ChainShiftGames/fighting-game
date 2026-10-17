using FightingGame.UI;
using Xunit;

namespace FightingGame.Tests.UI;

public sealed class PingReadingTests
{
    [Theory]
    [InlineData(0, PingQuality.Good)]
    [InlineData(100, PingQuality.Good)]
    [InlineData(101, PingQuality.Fair)]
    [InlineData(150, PingQuality.Fair)]
    [InlineData(151, PingQuality.Poor)]
    public void RatesPingByHowPlayableItIs(int ping, PingQuality expected) =>
        Assert.Equal(expected, new PingReading(ping).Quality);

    [Fact]
    public void ShowsThePingInMilliseconds() => Assert.Equal("Ping 85 ms", new PingReading(85).Text);
}
