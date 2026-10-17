using System;
using System.Buffers.Binary;
using System.IO;
using Netcode.Replays;
using Netcode.Tests.Matches;
using Sim.Composition;
using Sim.Data.Hashing;
using Sim.Rules;

namespace Netcode.Tests.Replays;

/// <summary>Replay headers for the real game data, and the match a replay's header describes.</summary>
internal static class RecordedMatches
{
    public const string GameVersion = "1.0.0+test";

    public static byte[] DataHash { get; } = BytesOf(TestmanMirror.Data.Hash);

    public static ReplayHeader Header(byte player1Colour = 0, byte player2Colour = 1)
    {
        MatchRules rules = TestmanMirror.Data.CreateMatchRules();
        return new ReplayHeader(
            GameVersion,
            DataHash,
            in rules,
            new ReplayPlayer(TestmanMirror.Character, player1Colour),
            new ReplayPlayer(TestmanMirror.Character, player2Colour));
    }

    /// <summary>Rebuilds the recorded match from the header, as the game does to play a replay back.</summary>
    public static MatchSimulation MatchOf(ReplayHeader header) => new(
        TestmanMirror.Data.Character(header.Player1.CharacterId),
        TestmanMirror.Data.Character(header.Player2.CharacterId),
        TestmanMirror.Data.Buttons,
        TestmanMirror.Data.Motions.Motions,
        TestmanMirror.Data.Motions.Movement,
        in header.Rules,
        TestmanMirror.Data.Flags.Names);

    public static byte[] Save(Replay replay)
    {
        using var file = new MemoryStream();
        ReplayWriter.Write(file, replay);
        return file.ToArray();
    }

    public static Replay Load(byte[] file, string gameVersion = GameVersion, byte[]? dataHash = null)
    {
        using var stream = new MemoryStream(file);
        return ReplayReader.Read(stream, gameVersion, dataHash ?? DataHash);
    }

    private static byte[] BytesOf(DataHash hash)
    {
        byte[] bytes = new byte[Sim.Data.Hashing.DataHash.ByteLength];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, hash.Part0);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(8), hash.Part1);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(16), hash.Part2);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(24), hash.Part3);
        return bytes;
    }
}
