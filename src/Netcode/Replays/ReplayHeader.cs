using System;
using System.Text;
using Sim.Rules;

namespace Netcode.Replays;

/// <summary>What a replay needs besides the inputs: the build that recorded it and the match it recorded (ADR-0018).</summary>
/// <remarks>
/// The game version and data hash decide whether this build may play the replay back; the match rules, characters
/// and colours rebuild the match it recorded.
/// </remarks>
public sealed class ReplayHeader
{
    /// <summary>Longest game version, character id (UTF-8) or data hash the file format can hold.</summary>
    public const int MaxFieldBytes = byte.MaxValue;

    private readonly byte[] _dataHash;
    private readonly MatchRules _rules;

    /// <param name="gameVersion">The build that recorded the match, e.g. "1.0.0+commit" (the online handshake's version).</param>
    /// <param name="dataHash">Hash of all game data the match was played with (ARCHITECTURE.md §7).</param>
    /// <param name="rules">The match rules it was played by; developer settings, fixed in shipped builds.</param>
    public ReplayHeader(
        string gameVersion, ReadOnlySpan<byte> dataHash, in MatchRules rules, ReplayPlayer player1, ReplayPlayer player2)
    {
        EnsureFits(gameVersion, nameof(gameVersion));
        EnsureFits(player1.CharacterId, nameof(player1));
        EnsureFits(player2.CharacterId, nameof(player2));
        if (dataHash.IsEmpty || dataHash.Length > MaxFieldBytes)
        {
            throw new ArgumentException($"A data hash is 1 to {MaxFieldBytes} bytes, got {dataHash.Length}.", nameof(dataHash));
        }

        GameVersion = gameVersion;
        _dataHash = dataHash.ToArray();
        _rules = rules;
        Player1 = player1;
        Player2 = player2;
    }

    public string GameVersion { get; }

    public ReadOnlySpan<byte> DataHash => _dataHash;

    public ref readonly MatchRules Rules => ref _rules;

    /// <summary>The same hash the online handshake compares (<see cref="MatchRules.ComputeHash"/>).</summary>
    public ulong RulesHash => _rules.ComputeHash();

    public ReplayPlayer Player1 { get; }

    public ReplayPlayer Player2 { get; }

    private static void EnsureFits(string text, string parameter)
    {
        int length = Encoding.UTF8.GetByteCount(text);
        if (length == 0 || length > MaxFieldBytes)
        {
            throw new ArgumentException($"'{text}' must be 1 to {MaxFieldBytes} UTF-8 bytes, got {length}.", parameter);
        }
    }
}
