using System;
using Netcode.Desync;
using Xunit;

namespace Netcode.Tests.Desync;

public sealed class ChecksumComparerTests
{
    [Fact]
    public void MatchingChecksumsMoveTheLastAgreedFrame()
    {
        var comparer = new ChecksumComparer(8);
        for (int frame = 1; frame <= 20; frame++)
        {
            Assert.False(comparer.AddLocal(new FrameChecksum(frame, (ulong)frame), out _));
            Assert.False(comparer.AddRemote(new FrameChecksum(frame, (ulong)frame), out _));
        }

        Assert.Equal(20, comparer.LastAgreedFrame);
    }

    [Fact]
    public void TheFirstDifferingFrameIsReportedWithTheLastAgreedOne()
    {
        var comparer = new ChecksumComparer(8);
        comparer.AddLocal(new FrameChecksum(1, 10), out _);
        comparer.AddRemote(new FrameChecksum(1, 10), out _);
        comparer.AddLocal(new FrameChecksum(2, 20), out _);

        Assert.True(comparer.AddRemote(new FrameChecksum(2, 21), out ChecksumMismatch mismatch));
        Assert.Equal(new ChecksumMismatch(2, 20, 21, 1), mismatch);
    }

    [Fact]
    public void ARemoteChecksumThatArrivesFirstWaitsForTheLocalOne()
    {
        var comparer = new ChecksumComparer(8);
        Assert.False(comparer.AddRemote(new FrameChecksum(1, 5), out _));
        Assert.False(comparer.AddRemote(new FrameChecksum(2, 6), out _));

        Assert.False(comparer.AddLocal(new FrameChecksum(1, 5), out _));
        Assert.True(comparer.AddLocal(new FrameChecksum(2, 7), out ChecksumMismatch mismatch));
        Assert.Equal(2, mismatch.Frame);
        Assert.Equal(1, mismatch.LastAgreedFrame);
    }

    [Fact]
    public void AFrameNeverComparedFailsLoudlyInsteadOfBeingSkipped()
    {
        var comparer = new ChecksumComparer(4);
        for (int frame = 1; frame <= 4; frame++)
        {
            comparer.AddLocal(new FrameChecksum(frame, 1), out _);
        }

        Assert.Throws<InvalidOperationException>(() => comparer.AddLocal(new FrameChecksum(5, 1), out _));
    }
}
