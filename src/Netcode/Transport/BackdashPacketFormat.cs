using System;
using System.Buffers.Binary;
using Backdash.Core;

namespace Netcode.Transport;

/// <summary>Tells whether a packet is exactly one complete Backdash protocol message, as Backdash writes it.</summary>
/// <remarks>
/// Backdash's receive loop only survives its own parse errors: a cut-off message throws something else and stops all
/// receiving for the rest of the match, and an input message with too many players writes past a buffer. So every
/// packet is checked here before Backdash reads it. This mirrors Backdash 0.7.8's wire format (big-endian numbers,
/// its default); <c>BackdashPacketFormatTests</c> checks it against a real session's traffic, so a Backdash upgrade
/// that changes the format fails there (ADR-0014).
/// </remarks>
internal static class BackdashPacketFormat
{
    // Every message starts with its type, sync number and sequence number.
    private const int HeaderSize = sizeof(ushort) * 3;

    private const int FrameSize = sizeof(int);
    private const int ChecksumSize = sizeof(uint);
    private const int TimestampSize = sizeof(long);
    private const int BitsPerByte = 8;

    // An input message starts with its start frame, ack frame, disconnect flag and player count, then one status
    // (disconnected flag, last frame) per player, then the input size and the bit count of the inputs that follow.
    private const int PlayerCountOffset = HeaderSize + FrameSize + FrameSize + sizeof(bool);
    private const int PlayerStatusSize = sizeof(bool) + FrameSize;
    private const int InputSizesSize = sizeof(byte) + sizeof(ushort);

    /// <summary>The largest message: an input message with every player and the fullest input buffer.</summary>
    public const int MaxMessageSize = PlayerCountOffset + sizeof(byte) +
        (NetcodeConstants.Max.NumberOfPlayers * PlayerStatusSize) + InputSizesSize + NetcodeConstants.Max.CompressedBytes;

    private const int UnknownType = -1;

    // Backdash's message type numbers.
    private enum MessageType : ushort
    {
        SyncRequest = 1,
        SyncReply = 2,
        Input = 3,
        QualityReport = 4,
        QualityReply = 5,
        KeepAlive = 6,
        InputAck = 7,
        ConsistencyCheckRequest = 8,
        ConsistencyCheckReply = 9,
        ConsistencyCheckFail = 10,
    }

    public static bool IsWellFormed(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < HeaderSize)
        {
            return false;
        }

        var type = (MessageType)BinaryPrimitives.ReadUInt16BigEndian(packet);
        if (type == MessageType.Input)
        {
            return IsWellFormedInput(packet);
        }

        int bodySize = FixedBodySize(type);
        return bodySize != UnknownType && packet.Length == HeaderSize + bodySize;
    }

    private static int FixedBodySize(MessageType type) => type switch
    {
        MessageType.SyncRequest => sizeof(ushort) + TimestampSize,
        MessageType.SyncReply => sizeof(uint) + TimestampSize,
        MessageType.QualityReport => sizeof(int) + TimestampSize,
        MessageType.QualityReply => TimestampSize,
        MessageType.KeepAlive => 0,
        MessageType.InputAck => FrameSize,
        MessageType.ConsistencyCheckRequest => FrameSize,
        MessageType.ConsistencyCheckReply => FrameSize + ChecksumSize,
        MessageType.ConsistencyCheckFail => FrameSize + ChecksumSize + ChecksumSize,
        _ => UnknownType,
    };

    private static bool IsWellFormedInput(ReadOnlySpan<byte> packet)
    {
        if (packet.Length <= PlayerCountOffset)
        {
            return false;
        }

        int players = packet[PlayerCountOffset];
        if (players > NetcodeConstants.Max.NumberOfPlayers)
        {
            return false;
        }

        int sizesOffset = PlayerCountOffset + sizeof(byte) + (players * PlayerStatusSize);
        if (packet.Length < sizesOffset + InputSizesSize)
        {
            return false;
        }

        int bitCount = BinaryPrimitives.ReadUInt16BigEndian(packet[(sizesOffset + sizeof(byte))..]);
        int inputBytes = (bitCount + BitsPerByte - 1) / BitsPerByte;
        return inputBytes <= NetcodeConstants.Max.CompressedBytes &&
            packet.Length == sizesOffset + InputSizesSize + inputBytes;
    }
}
