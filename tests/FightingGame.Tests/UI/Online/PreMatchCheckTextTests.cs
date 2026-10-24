using System;
using FightingGame.Tests.UI.Framework;
using FightingGame.UI.Online;
using Netcode.Handshake;
using Xunit;

namespace FightingGame.Tests.UI.Online;

public sealed class PreMatchCheckTextTests
{
    [Fact]
    public void NothingIsSaidWhenTheChecksPassed() => Assert.Empty(PreMatchCheckText.For(HandshakeRefusal.None));

    [Fact]
    public void ADifferentVersionIsSaidPlainly()
    {
        string text = MenuTextTable.Formatter.FormatLines(PreMatchCheckText.For(HandshakeRefusal.GameVersion));

        Assert.Equal(
            "The match can't start:\nYour opponent has a different game version.\n" +
            "Both players need the same version of the game: update and try again.",
            text);
    }

    [Fact]
    public void EachDifferenceGetsALineAndAnotherHandshakeCountsAsAnotherVersion()
    {
        string text = MenuTextTable.Formatter.FormatLines(PreMatchCheckText.For(
            HandshakeRefusal.GameVersion | HandshakeRefusal.HandshakeFormat | HandshakeRefusal.GameData |
            HandshakeRefusal.MatchRules));

        // Said once: two parts around one occurrence.
        Assert.Equal(2, text.Split("different game version").Length);
        Assert.Contains("different game data", text, StringComparison.Ordinal);
        Assert.Contains("different match rules", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ARefusedMatchOffersNoRematch()
    {
        Assert.False(OnlineResultsText.CanRematch(HandshakeRefusal.GameData));
        Assert.True(OnlineResultsText.CanRematch(HandshakeRefusal.None));
    }
}
