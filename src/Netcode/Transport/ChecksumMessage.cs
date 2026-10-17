using System;
using System.Buffers.Binary;

namespace Netcode.Transport;

/// <summary>
/// The desync check's packet (ADR-0019): the checksums of this PC's confirmed frames the other PC hasn't acknowledged
/// yet, and up to which frame this PC has received the other PC's.
/// </summary>
/// <remarks>
/// Layout, big-endian: marker, format, acknowledged frame, first frame, count, then <c>count</c> checksums of the
/// frames from the first one on. The marker is no Backdash message type and not the hello's, so neither the netcode
/// nor the handshake mistakes it for theirs.
/// </remarks>
internal static class ChecksumMessage
{
    /// <summary>"FC"; Backdash's message types are 0–10, the hello's marker is "FG".</summary>
    public const ushort Marker = 0x4643;

    /// <summary>Bumped when the layout changes; another format is dropped as malformed (same build only, ADR-0005 §6).</summary>
    public const byte Format = 1;

    /// <summary>Most checksums in one packet: about two seconds of frames, well inside a packet.</summary>
    public const int MaxChecksums = 128;

    private const int FormatOffset = sizeof(ushort);
    private const int AcknowledgedOffset = FormatOffset + sizeof(byte);
    private const int FirstFrameOffset = AcknowledgedOffset + sizeof(int);
    private const int CountOffset = FirstFrameOffset + sizeof(int);
    private const int ChecksumsOffset = CountOffset + sizeof(byte);

    /// <summary>The largest packet, with <see cref="MaxChecksums"/> checksums; within <see cref="ITransport.MaxPacketSize"/>.</summary>
    public const int MaxSize = ChecksumsOffset + (MaxChecksums * sizeof(ulong));

    public static bool IsChecksumMessage(ReadOnlySpan<byte> packet) =>
        packet.Length >= sizeof(ushort) && BinaryPrimitives.ReadUInt16BigEndian(packet) == Marker;

    /// <param name="acknowledged">The newest frame of the other PC's this PC has received, with all before it; 0 for none.</param>
    /// <param name="firstFrame">The frame of the first checksum; ignored when there are none.</param>
    /// <returns>The packet's length.</returns>
    public static int Write(int acknowledged, int firstFrame, ReadOnlySpan<ulong> checksums, Span<byte> packet)
    {
        BinaryPrimitives.WriteUInt16BigEndian(packet, Marker);
        packet[FormatOffset] = Format;
        BinaryPrimitives.WriteInt32BigEndian(packet[AcknowledgedOffset..], acknowledged);
        BinaryPrimitives.WriteInt32BigEndian(packet[FirstFrameOffset..], checksums.Length > 0 ? firstFrame : 0);
        packet[CountOffset] = (byte)checksums.Length;
        for (int i = 0; i < checksums.Length; i++)
        {
            BinaryPrimitives.WriteUInt64BigEndian(packet[(ChecksumsOffset + (i * sizeof(ulong)))..], checksums[i]);
        }

        return ChecksumsOffset + (checksums.Length * sizeof(ulong));
    }

    /// <summary>Reads a packet of this format; false when it's cut off, of another format or its numbers don't add up.</summary>
    /// <param name="count">How many checksums follow; read each with <see cref="ChecksumAt"/>.</param>
    public static bool TryRead(ReadOnlySpan<byte> packet, out int acknowledged, out int firstFrame, out int count)
    {
        acknowledged = 0;
        firstFrame = 0;
        count = 0;
        if (!IsChecksumMessage(packet) || packet.Length < ChecksumsOffset || packet[FormatOffset] != Format)
        {
            return false;
        }

        acknowledged = BinaryPrimitives.ReadInt32BigEndian(packet[AcknowledgedOffset..]);
        firstFrame = BinaryPrimitives.ReadInt32BigEndian(packet[FirstFrameOffset..]);
        count = packet[CountOffset];
        return acknowledged >= 0 &&
            count <= MaxChecksums &&
            packet.Length == ChecksumsOffset + (count * sizeof(ulong)) &&
            (count == 0 || (firstFrame >= 1 && firstFrame <= int.MaxValue - count));
    }

    /// <summary>The checksum at <paramref name="index"/> of a packet <see cref="TryRead"/> accepted.</summary>
    public static ulong ChecksumAt(ReadOnlySpan<byte> packet, int index) =>
        BinaryPrimitives.ReadUInt64BigEndian(packet[(ChecksumsOffset + (index * sizeof(ulong)))..]);
}
