using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Netcode.Handshake;

/// <summary>
/// The handshake's only packet: this PC's <see cref="MatchIdentity"/>, and whether it has seen the other PC's hello.
/// </summary>
/// <remarks>
/// Layout: marker, format, flags, rules hash, then the data hash and the game version, each after its length byte.
/// Numbers are big-endian. The marker is no Backdash message type, so a hello is never mistaken for netcode traffic.
/// </remarks>
internal static class HelloMessage
{
    /// <summary>"FG"; Backdash's message types are 0–10.</summary>
    public const ushort Marker = 0x4647;

    /// <summary>Bumped when the layout changes; a different format can't be read, so it refuses the match.</summary>
    public const byte Format = 1;

    private const int FormatOffset = sizeof(ushort);
    private const int FlagsOffset = FormatOffset + sizeof(byte);
    private const int RulesHashOffset = FlagsOffset + sizeof(byte);
    private const int DataHashOffset = RulesHashOffset + sizeof(ulong);
    private const byte SeenYoursFlag = 1;

    public static bool IsHello(ReadOnlySpan<byte> packet) =>
        packet.Length >= sizeof(ushort) && BinaryPrimitives.ReadUInt16BigEndian(packet) == Marker;

    /// <summary>The hello's format, or -1 when the packet is too short to say.</summary>
    public static int FormatOf(ReadOnlySpan<byte> packet) => packet.Length > FormatOffset ? packet[FormatOffset] : -1;

    /// <returns>The hello's length; it always fits in a packet (<see cref="MatchIdentity.MaxFieldBytes"/>).</returns>
    public static int Write(MatchIdentity identity, bool seenYours, Span<byte> packet)
    {
        BinaryPrimitives.WriteUInt16BigEndian(packet, Marker);
        packet[FormatOffset] = Format;
        packet[FlagsOffset] = seenYours ? SeenYoursFlag : (byte)0;
        BinaryPrimitives.WriteUInt64BigEndian(packet[RulesHashOffset..], identity.RulesHash);

        int offset = DataHashOffset;
        packet[offset++] = (byte)identity.DataHash.Length;
        identity.DataHash.CopyTo(packet[offset..]);
        offset += identity.DataHash.Length;

        int versionLength = Encoding.UTF8.GetBytes(identity.GameVersion, packet[(offset + 1)..]);
        packet[offset++] = (byte)versionLength;
        return offset + versionLength;
    }

    /// <summary>Reads a hello of this format; false when it's cut off or its lengths don't add up.</summary>
    public static bool TryRead(
        ReadOnlySpan<byte> packet, [NotNullWhen(true)] out MatchIdentity? identity, out bool seenYours)
    {
        identity = null;
        seenYours = false;
        if (!IsHello(packet) || FormatOf(packet) != Format || packet.Length <= DataHashOffset)
        {
            return false;
        }

        int dataHashLength = packet[DataHashOffset];
        int versionLengthOffset = DataHashOffset + 1 + dataHashLength;
        if (packet.Length <= versionLengthOffset || packet.Length != versionLengthOffset + 1 + packet[versionLengthOffset])
        {
            return false;
        }

        string gameVersion;
        try
        {
            gameVersion = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(packet[(versionLengthOffset + 1)..]);
        }
        catch (ArgumentException)
        {
            return false;
        }

        byte[] dataHash = packet.Slice(DataHashOffset + 1, dataHashLength).ToArray();
        identity = new MatchIdentity(gameVersion, dataHash, BinaryPrimitives.ReadUInt64BigEndian(packet[RulesHashOffset..]));
        seenYours = (packet[FlagsOffset] & SeenYoursFlag) != 0;
        return true;
    }
}
