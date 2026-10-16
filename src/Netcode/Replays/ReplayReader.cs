using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Netcode.Drivers;
using Sim.Input;
using Sim.Rules;

namespace Netcode.Replays;

/// <summary>Loads a replay file (<see cref="ReplayFormat"/>) that this build can play back, or says why it can't.</summary>
/// <remarks>
/// Only the build that recorded a replay plays it back (ADR-0018): another game version or other game data would
/// replay the inputs into a different match. Such files are rejected before their inputs are read.
/// </remarks>
public static class ReplayReader
{
    private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <param name="gameVersion">This build's version, as the recorder stored it.</param>
    /// <param name="dataHash">This build's game data hash, as the recorder stored it.</param>
    /// <exception cref="ReplayRejectedException">This build can't play the file; the message says why.</exception>
    /// <exception cref="IOException">The stream itself failed.</exception>
    public static Replay Read(Stream stream, string gameVersion, ReadOnlySpan<byte> dataHash)
    {
        try
        {
            using (var plain = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                EnsureReplayOfThisFormat(plain);
            }

            using var compressed = new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: true);
            using var reader = new BinaryReader(compressed, Encoding.UTF8, leaveOpen: true);
            ReplayHeader header = ReadHeader(reader, gameVersion, dataHash);
            int frameCount = reader.ReadInt32();
            if (frameCount < 0 || frameCount > ReplayFormat.MaxFrames)
            {
                throw Damaged();
            }

            FrameInputs[] inputs = ReadInputRuns(reader, frameCount);

            // Reading to the end also checks the zlib checksum over everything after the format.
            return compressed.ReadByte() == -1 ? new Replay(header, inputs) : throw Damaged();
        }
        catch (Exception exception) when (
            exception is EndOfStreamException or InvalidDataException or FormatException or DecoderFallbackException)
        {
            throw Damaged(exception);
        }
    }

    private static void EnsureReplayOfThisFormat(BinaryReader reader)
    {
        if (!reader.ReadBytes(ReplayFormat.Magic.Length).AsSpan().SequenceEqual(ReplayFormat.Magic))
        {
            throw new ReplayRejectedException(ReplayRejection.NotAReplay, "This file isn't a replay.");
        }

        ushort format = reader.ReadUInt16();
        if (format != ReplayFormat.Version)
        {
            throw new ReplayRejectedException(
                ReplayRejection.OtherFormat,
                $"This replay was saved in replay format {format}; this version of the game reads format " +
                $"{ReplayFormat.Version}.");
        }
    }

    private static ReplayHeader ReadHeader(BinaryReader reader, string gameVersion, ReadOnlySpan<byte> dataHash)
    {
        string recordedVersion = _strictUtf8.GetString(ReadField(reader));
        if (recordedVersion != gameVersion)
        {
            throw new ReplayRejectedException(
                ReplayRejection.OtherGameVersion,
                $"This replay was recorded with game version {recordedVersion}; this is version {gameVersion}. " +
                "Replays only play on the version that recorded them.");
        }

        byte[] recordedDataHash = ReadField(reader);
        if (!recordedDataHash.AsSpan().SequenceEqual(dataHash))
        {
            throw new ReplayRejectedException(
                ReplayRejection.OtherGameData,
                "This replay was recorded with different game data (characters, moves or rules), so it can't be " +
                "played back here.");
        }

        ulong rulesHash = reader.ReadUInt64();
        MatchRules rules = ReadRules(reader, rulesHash);
        ReplayPlayer player1 = ReadPlayer(reader);
        ReplayPlayer player2 = ReadPlayer(reader);
        return new ReplayHeader(recordedVersion, recordedDataHash, in rules, player1, player2);
    }

    // The same build wrote these bytes from the same struct; a size or hash that doesn't match means damage.
    private static MatchRules ReadRules(BinaryReader reader, ulong rulesHash)
    {
        int size = reader.ReadInt32();
        if (size != Unsafe.SizeOf<MatchRules>())
        {
            throw Damaged();
        }

        byte[] bytes = reader.ReadBytes(size);
        if (bytes.Length != size)
        {
            throw new EndOfStreamException();
        }

        MatchRules rules = MemoryMarshal.Read<MatchRules>(bytes);
        return rules.ComputeHash() == rulesHash ? rules : throw Damaged();
    }

    private static ReplayPlayer ReadPlayer(BinaryReader reader)
    {
        string characterId = _strictUtf8.GetString(ReadField(reader));
        return new ReplayPlayer(characterId, reader.ReadByte());
    }

    // A length byte, then that many bytes; every field has at least one.
    private static byte[] ReadField(BinaryReader reader)
    {
        byte length = reader.ReadByte();
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new EndOfStreamException();
        }

        return length == 0 ? throw Damaged() : bytes;
    }

    // The frame count is at most ReplayFormat.MaxFrames, so even a damaged one allocates a bounded array.
    private static FrameInputs[] ReadInputRuns(BinaryReader reader, int frameCount)
    {
        var frames = new FrameInputs[frameCount];
        int filled = 0;
        while (filled < frameCount)
        {
            int run = reader.Read7BitEncodedInt();
            if (run <= 0 || run > frameCount - filled)
            {
                throw Damaged();
            }

            var inputs = new FrameInputs(new InputWord(reader.ReadUInt16()), new InputWord(reader.ReadUInt16()));
            frames.AsSpan(filled, run).Fill(inputs);
            filled += run;
        }

        return frames;
    }

    private static ReplayRejectedException Damaged(Exception? cause = null) =>
        new(ReplayRejection.Damaged, "This replay file is damaged and can't be played back.", cause);
}
