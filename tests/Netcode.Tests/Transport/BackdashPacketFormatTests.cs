using System;
using System.Buffers.Binary;
using Netcode.Transport;
using Xunit;

namespace Netcode.Tests.Transport;

/// <summary>
/// P2-03: the packet check accepts exactly what Backdash sends. Its real traffic is the reference, so a Backdash
/// upgrade that changes the wire format fails here first (ADR-0014).
/// </summary>
public sealed class BackdashPacketFormatTests : IClassFixture<RealSessionTraffic>
{
    // Input message layout: header (6), start frame, ack frame, disconnect flag, then the player count.
    private const int PlayerCountOffset = 15;
    private const int PlayerStatusSize = 5;
    private const int MaxPlayers = 4;
    private const int MaxInputBytes = 512;

    private readonly RealSessionTraffic _traffic;

    public BackdashPacketFormatTests(RealSessionTraffic traffic)
    {
        _traffic = traffic;
    }

    [Fact]
    public void EveryPacketARealSessionSendsIsWellFormed()
    {
        Assert.All(_traffic.Packets, packet => Assert.True(
            BackdashPacketFormat.IsWellFormed(packet),
            $"Type {RealSessionTraffic.TypeOf(packet)}, {packet.Length} bytes: {Convert.ToHexString(packet)}"));
    }

    [Fact]
    public void ACutOffMessageIsMalformed()
    {
        Assert.All(_traffic.OnePerType(), packet =>
        {
            for (int length = 0; length < packet.Length; length++)
            {
                Assert.False(BackdashPacketFormat.IsWellFormed(packet.AsSpan(0, length)), $"{length} of {packet.Length} bytes");
            }
        });
    }

    [Fact]
    public void AMessageWithBytesLeftOverIsMalformed()
    {
        Assert.All(_traffic.OnePerType(), packet => Assert.False(BackdashPacketFormat.IsWellFormed([.. packet, 0])));
    }

    [Fact]
    public void AnInputMessageMayCarryEveryPlayerAndAFullInputBuffer()
    {
        Assert.True(BackdashPacketFormat.IsWellFormed(InputMessage(MaxPlayers, MaxInputBytes * 8)));
    }

    // Backdash reads player statuses into a 4-entry buffer without a bounds check.
    [Theory]
    [InlineData(MaxPlayers + 1)]
    [InlineData(byte.MaxValue)]
    public void AnInputMessageWithMorePlayersThanBackdashHoldsIsMalformed(int players)
    {
        Assert.False(BackdashPacketFormat.IsWellFormed(InputMessage(players, 16)));
    }

    [Fact]
    public void AnInputMessageWithMoreInputsThanBackdashHoldsIsMalformed()
    {
        Assert.False(BackdashPacketFormat.IsWellFormed(InputMessage(2, (MaxInputBytes * 8) + 1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(ushort.MaxValue)]
    public void AnUnknownMessageTypeIsMalformed(int type)
    {
        byte[] packet = new byte[6];
        BinaryPrimitives.WriteUInt16BigEndian(packet, (ushort)type);

        Assert.False(BackdashPacketFormat.IsWellFormed(packet));
    }

    [Fact]
    public void EveryWellFormedMessageFitsInAPacket()
    {
        Assert.True(BackdashPacketFormat.MaxMessageSize <= ITransport.MaxPacketSize);
    }

    // A complete input message as the layout reads it; every other field is zero.
    private static byte[] InputMessage(int players, int bitCount)
    {
        int sizesOffset = PlayerCountOffset + 1 + (players * PlayerStatusSize);
        byte[] packet = new byte[sizesOffset + 3 + ((bitCount + 7) / 8)];
        BinaryPrimitives.WriteUInt16BigEndian(packet, RealSessionTraffic.Input);
        packet[PlayerCountOffset] = (byte)players;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(sizesOffset + 1), (ushort)bitCount);
        return packet;
    }
}
