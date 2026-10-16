using System;

namespace Netcode.Replays;

/// <summary>The replay file layout shared by <see cref="ReplayWriter"/> and <see cref="ReplayReader"/> (ADR-0018).</summary>
/// <remarks>
/// Little-endian. Uncompressed: magic and format, so any build can tell what a file is. The rest is zlib compressed,
/// and its checksum catches damage: game version, data hash, rules hash, rules size and bytes, P1 and P2 (character
/// id, colour), frame count, then the inputs as runs of frames with the same inputs, each its length (7-bit encoded)
/// and P1's and P2's input words. Text and hashes follow their length byte.
/// </remarks>
internal static class ReplayFormat
{
    /// <summary>Raised when the layout changes; a replay of another format is rejected, never converted.</summary>
    public const ushort Version = 1;

    /// <summary>
    /// Most frames a replay may hold: about 77 hours, far beyond any match, yet small enough that a damaged frame
    /// count can't make the reader run out of memory.
    /// </summary>
    public const int MaxFrames = 1 << 24;

    public static ReadOnlySpan<byte> Magic => "FGRP"u8;
}
