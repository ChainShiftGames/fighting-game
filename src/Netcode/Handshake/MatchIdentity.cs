using System;
using System.Collections.Generic;
using System.Text;
using Sim.Rules;

namespace Netcode.Handshake;

/// <summary>What both PCs must share before an online match: the same build, game data and match rules (ADR-0005 §6).</summary>
public sealed class MatchIdentity
{
    /// <summary>Longest game version (UTF-8) and data hash a hello can carry.</summary>
    public const int MaxFieldBytes = byte.MaxValue;

    private readonly byte[] _dataHash;

    /// <param name="gameVersion">The build, e.g. "1.0.0+commit"; two different builds never play each other.</param>
    /// <param name="dataHash">Hash of all game data (ARCHITECTURE.md §7).</param>
    /// <param name="rules">The match rules this PC will play by, compared by <see cref="MatchRules.ComputeHash"/>.</param>
    public MatchIdentity(string gameVersion, ReadOnlySpan<byte> dataHash, in MatchRules rules)
        : this(gameVersion, dataHash.ToArray(), rules.ComputeHash())
    {
    }

    internal MatchIdentity(string gameVersion, byte[] dataHash, ulong rulesHash)
    {
        if (Encoding.UTF8.GetByteCount(gameVersion) > MaxFieldBytes || dataHash.Length > MaxFieldBytes)
        {
            throw new ArgumentException($"The game version and data hash may be at most {MaxFieldBytes} bytes each.");
        }

        GameVersion = gameVersion;
        _dataHash = dataHash;
        RulesHash = rulesHash;
    }

    public string GameVersion { get; }

    public ReadOnlySpan<byte> DataHash => _dataHash;

    /// <summary>The same hash the P1-20 test checks changes with every rules field (ADR-0011 §7).</summary>
    public ulong RulesHash { get; }

    /// <summary>Each part that differs from <paramref name="other"/>; <see cref="HandshakeRefusal.None"/> when none does.</summary>
    public HandshakeRefusal RefusalFrom(MatchIdentity other)
    {
        HandshakeRefusal refusal = HandshakeRefusal.None;
        if (!string.Equals(GameVersion, other.GameVersion, StringComparison.Ordinal))
        {
            refusal |= HandshakeRefusal.GameVersion;
        }

        if (!DataHash.SequenceEqual(other.DataHash))
        {
            refusal |= HandshakeRefusal.GameData;
        }

        if (RulesHash != other.RulesHash)
        {
            refusal |= HandshakeRefusal.MatchRules;
        }

        return refusal;
    }

    /// <summary>Each part that differs from <paramref name="other"/>, described for the devs; empty when none does.</summary>
    public IReadOnlyList<string> DifferencesFrom(MatchIdentity other)
    {
        var differences = new List<string>();
        if (!string.Equals(GameVersion, other.GameVersion, StringComparison.Ordinal))
        {
            differences.Add($"game version {GameVersion} here, {other.GameVersion} there");
        }

        if (!DataHash.SequenceEqual(other.DataHash))
        {
            differences.Add("game data differs");
        }

        if (RulesHash != other.RulesHash)
        {
            differences.Add("match rules differ");
        }

        return differences;
    }
}
