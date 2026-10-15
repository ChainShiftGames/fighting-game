using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Platform.Steam.Transport;

/// <summary>A Steam user as a network address, so Backdash can send to them like to any other peer.</summary>
/// <remarks>
/// Backdash tells peers apart by the bytes of a <see cref="SocketAddress"/> and receives into IPv4-sized ones.
/// A Steam user has no IP address, so their 64-bit Steam ID goes in the 8 spare bytes of an IPv4 address
/// (where <c>sockaddr_in</c> has padding). Only <see cref="SteamMessagesSocket"/> reads it back.
/// </remarks>
public sealed class SteamPeerEndPoint : EndPoint
{
    private const int AddressSize = 16;
    private const int SteamIdOffset = 8;

    public SteamPeerEndPoint(ulong steamId)
    {
        SteamId = steamId;
    }

    public ulong SteamId { get; }

    public override AddressFamily AddressFamily => AddressFamily.InterNetwork;

    public static ulong ReadSteamId(SocketAddress address) =>
        BinaryPrimitives.ReadUInt64LittleEndian(address.Buffer.Span[SteamIdOffset..AddressSize]);

    public static void WriteSteamId(SocketAddress address, ulong steamId)
    {
        address.Buffer.Span[2..AddressSize].Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(address.Buffer.Span[SteamIdOffset..AddressSize], steamId);
    }

    public override SocketAddress Serialize()
    {
        var address = new SocketAddress(AddressFamily.InterNetwork, AddressSize);
        WriteSteamId(address, SteamId);
        return address;
    }

    public override EndPoint Create(SocketAddress socketAddress) => new SteamPeerEndPoint(ReadSteamId(socketAddress));

    public override bool Equals(object? obj) => obj is SteamPeerEndPoint other && other.SteamId == SteamId;

    public override int GetHashCode() => SteamId.GetHashCode();

    public override string ToString() => $"steam:{SteamId}";
}
