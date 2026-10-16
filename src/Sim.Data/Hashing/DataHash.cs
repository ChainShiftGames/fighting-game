using System;
using System.Buffers.Binary;

namespace Sim.Data.Hashing;

/// <summary>
/// SHA-256 of all game data in compact form. Online players compare it before a match and replays store it
/// (ARCHITECTURE.md §7–8). Fixed-size, so it can live in plain data such as a replay header.
/// </summary>
public readonly record struct DataHash(ulong Part0, ulong Part1, ulong Part2, ulong Part3)
{
    public const int ByteLength = 32;

    public static DataHash FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ByteLength)
        {
            throw new ArgumentException($"A data hash is {ByteLength} bytes, got {bytes.Length}.", nameof(bytes));
        }

        return new DataHash(
            BinaryPrimitives.ReadUInt64BigEndian(bytes),
            BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]),
            BinaryPrimitives.ReadUInt64BigEndian(bytes[16..]),
            BinaryPrimitives.ReadUInt64BigEndian(bytes[24..]));
    }

    /// <summary>Lowercase hex, the same as common SHA-256 tools print.</summary>
    public override string ToString() => $"{Part0:x16}{Part1:x16}{Part2:x16}{Part3:x16}";
}
