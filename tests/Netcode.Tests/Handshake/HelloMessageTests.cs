using System;
using Netcode.Handshake;
using Netcode.Transport;
using Xunit;

namespace Netcode.Tests.Handshake;

/// <summary>P2-05: a hello reads back exactly as written, and anything else is not a hello.</summary>
public sealed class HelloMessageTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AHelloReadsBackAsWritten(bool seenYours)
    {
        MatchIdentity written = SameBuild.Identity();

        Assert.True(HelloMessage.TryRead(Hello(written, seenYours), out MatchIdentity? read, out bool readSeenYours));

        Assert.Empty(written.DifferencesFrom(read));
        Assert.Equal(seenYours, readSeenYours);
    }

    [Fact]
    public void ACutOffOrPaddedHelloIsNotRead()
    {
        byte[] hello = Hello(SameBuild.Identity(), true);

        for (int length = 0; length < hello.Length; length++)
        {
            Assert.False(HelloMessage.TryRead(hello.AsSpan(0, length), out _, out _), $"{length} of {hello.Length} bytes");
        }

        Assert.False(HelloMessage.TryRead([.. hello, 0], out _, out _));
    }

    [Fact]
    public void AGameVersionThatIsNotUtf8IsNotRead()
    {
        byte[] hello = Hello(new MatchIdentity("v", [], default), false);
        hello[^1] = 0xFF;

        Assert.False(HelloMessage.TryRead(hello, out _, out _));
    }

    [Fact]
    public void AHelloIsNeverANetcodeMessage()
    {
        Assert.False(BackdashPacketFormat.IsWellFormed(Hello(SameBuild.Identity(), false)));
    }

    [Fact]
    public void TheLongestHelloFitsInAPacket()
    {
        string longestVersion = new('v', MatchIdentity.MaxFieldBytes);
        byte[] longestHash = new byte[MatchIdentity.MaxFieldBytes];

        Assert.True(Hello(new MatchIdentity(longestVersion, longestHash, default), false).Length <= ITransport.MaxPacketSize);
    }

    private static byte[] Hello(MatchIdentity identity, bool seenYours)
    {
        byte[] packet = new byte[ITransport.MaxPacketSize];
        return packet.AsSpan(0, HelloMessage.Write(identity, seenYours, packet)).ToArray();
    }
}
