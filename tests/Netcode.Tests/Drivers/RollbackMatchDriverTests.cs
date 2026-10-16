using System;
using System.Diagnostics;
using Netcode.Tests.Transport;
using Xunit;

namespace Netcode.Tests.Drivers;

/// <summary>The rollback driver in an online match: what it does when the other PC goes away.</summary>
public sealed class RollbackMatchDriverTests
{
    private const int FightingFrames = 600;
    private const int CutFrame = 60;

    // The rollback window and input delay let each PC run a few frames past the last input it got, never more.
    private const int MostFramesPastTheCut = 40;

    // Backdash gives up on a silent opponent after 5 s (its DisconnectTimeout); this waits well past that.
    private static readonly TimeSpan _pastTheDisconnect = TimeSpan.FromSeconds(7);

    // Backdash itself would play on alone with neutral inputs for the other player, so both PCs would drift apart
    // without anyone noticing.
    [Fact]
    public void AMatchHoldsOnceTheOtherPcDisconnects()
    {
        using var match = new InMemoryMatch(FightingFrames);
        match.RunUntilFrame(CutFrame);

        match.Player1Transport.Cut();
        match.Player2Transport.Cut();
        var clock = Stopwatch.StartNew();
        match.RunUntil(() => clock.Elapsed > _pastTheDisconnect);

        Assert.InRange(match.Player1.CurrentFrame, CutFrame, CutFrame + MostFramesPastTheCut);
        Assert.InRange(match.Player2.CurrentFrame, CutFrame, CutFrame + MostFramesPastTheCut);
    }
}
