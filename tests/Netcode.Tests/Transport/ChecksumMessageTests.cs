using System;
using System.Buffers.Binary;
using Netcode.Transport;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>ADR-0019: the desync check's packet reads back what was written and refuses anything else.</summary>
public sealed class ChecksumMessageTests
{
    [Fact]
    public void ReadsBackWhatWasWritten()
    {
        byte[] packet = Write(acknowledged: 41, firstFrame: 7, 0x0102030405060708, ulong.MaxValue);

        Assert.True(ChecksumMessage.TryRead(packet, out int acknowledged, out int firstFrame, out int count));

        Assert.Equal((41, 7, 2), (acknowledged, firstFrame, count));
        Assert.Equal(0x0102030405060708UL, ChecksumMessage.ChecksumAt(packet, 0));
        Assert.Equal(ulong.MaxValue, ChecksumMessage.ChecksumAt(packet, 1));
    }

    [Fact]
    public void AnAcknowledgementAloneIsAPacketToo()
    {
        byte[] packet = Write(acknowledged: 12, firstFrame: 0);

        Assert.True(ChecksumMessage.TryRead(packet, out int acknowledged, out _, out int count));
        Assert.Equal((12, 0), (acknowledged, count));
    }

    [Fact]
    public void TheFullestPacketFitsInATransportPacket()
    {
        Assert.True(ChecksumMessage.MaxSize <= ITransport.MaxPacketSize);
        byte[] packet = Write(acknowledged: 0, firstFrame: 1, new ulong[ChecksumMessage.MaxChecksums]);

        Assert.Equal(ChecksumMessage.MaxSize, packet.Length);
        Assert.True(ChecksumMessage.TryRead(packet, out _, out _, out int count));
        Assert.Equal(ChecksumMessage.MaxChecksums, count);
    }

    [Fact]
    public void NeitherABackdashPacketNorAHelloLooksLikeOne()
    {
        // A keep-alive (type 6) and a hello ("FG").
        Assert.False(ChecksumMessage.IsChecksumMessage([0, 6, 0, 0, 0, 0]));
        Assert.False(ChecksumMessage.IsChecksumMessage([0x46, 0x47, 1]));
        Assert.True(ChecksumMessage.IsChecksumMessage(Write(acknowledged: 0, firstFrame: 1, 5)));
    }

    [Fact]
    public void ACutOffOrPaddedPacketIsRefused()
    {
        byte[] packet = Write(acknowledged: 0, firstFrame: 1, 5, 6);

        Assert.False(ChecksumMessage.TryRead(packet.AsSpan(0, packet.Length - 1), out _, out _, out _));
        Assert.False(ChecksumMessage.TryRead([.. packet, 0], out _, out _, out _));
        Assert.False(ChecksumMessage.TryRead(packet.AsSpan(0, 3), out _, out _, out _));
    }

    [Fact]
    public void AnotherFormatIsRefused()
    {
        byte[] packet = Write(acknowledged: 0, firstFrame: 1, 5);
        packet[2] = ChecksumMessage.Format + 1;

        Assert.False(ChecksumMessage.TryRead(packet, out _, out _, out _));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(0, -5)]
    [InlineData(0, int.MaxValue)]
    public void NumbersThatDontAddUpAreRefused(int acknowledged, int firstFrame)
    {
        byte[] packet = Write(acknowledged: 0, firstFrame: 1, 5, 6);
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(3), acknowledged);
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(7), firstFrame);

        Assert.False(ChecksumMessage.TryRead(packet, out _, out _, out _));
    }

    [Fact]
    public void MoreChecksumsThanAPacketMayHoldAreRefused()
    {
        byte[] packet = new byte[ChecksumMessage.MaxSize + sizeof(ulong)];
        ChecksumMessage.Write(0, 1, new ulong[ChecksumMessage.MaxChecksums], packet);
        packet[11] = ChecksumMessage.MaxChecksums + 1;

        Assert.False(ChecksumMessage.TryRead(packet, out _, out _, out _));
    }

    private static byte[] Write(int acknowledged, int firstFrame, params ulong[] checksums)
    {
        byte[] packet = new byte[ChecksumMessage.MaxSize];
        int length = ChecksumMessage.Write(acknowledged, firstFrame, checksums, packet);
        return packet[..length];
    }
}
