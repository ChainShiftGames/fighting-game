using System.IO;
using System.Net;
using FightingGame.Online;
using Netcode.Online;
using Xunit;

namespace FightingGame.Tests.Online;

public sealed class OnlinePickTextTests
{
    [Fact]
    public void APickRoundTripsThroughItsText()
    {
        var pick = new FighterPick("testman", 3);

        string text = OnlinePickText.Write(pick);

        Assert.Equal("3/testman", text);
        Assert.Equal(pick, OnlinePickText.TryRead(text));
        Assert.Equal(pick, OnlinePickText.Read(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("testman")]
    [InlineData("/testman")]
    [InlineData("3/")]
    [InlineData("300/testman")]
    [InlineData("-1/testman")]
    public void TextThatIsNoPickIsNotRead(string text) => Assert.Null(OnlinePickText.TryRead(text));

    [Fact]
    public void NoPickPlaysTheDefaultLineupAndAnUnreadablePickStopsTheMatch()
    {
        Assert.Equal(FighterPick.Default, OnlinePickText.Read(string.Empty));
        Assert.Throws<InvalidDataException>(() => OnlinePickText.Read("garbage"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BothPcsPutTheHostsPickOnPlayer1(bool hostIsLocal)
    {
        var opponent = new OnlineOpponent(new IPEndPoint(IPAddress.Loopback, 7000), "Rival", hostIsLocal, PeerTransport.DirectUdp(), 0);

        OnlineOpponent withPicks = hostIsLocal
            ? opponent.WithPicks(localPick: "0/host", opponentPick: "1/guest")
            : opponent.WithPicks(localPick: "1/guest", opponentPick: "0/host");

        Assert.Equal("0/host", withPicks.Player1Pick);
        Assert.Equal("1/guest", withPicks.Player2Pick);
    }
}
