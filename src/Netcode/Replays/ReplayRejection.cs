namespace Netcode.Replays;

/// <summary>Why a replay file can't be played back on this build.</summary>
public enum ReplayRejection
{
    /// <summary>The file isn't a replay at all.</summary>
    NotAReplay,

    /// <summary>A replay file layout this build doesn't read.</summary>
    OtherFormat,

    /// <summary>Recorded by another game version; it would play differently here (ADR-0018).</summary>
    OtherGameVersion,

    /// <summary>Recorded with other game data (characters, moves, rules); it would play differently here.</summary>
    OtherGameData,

    /// <summary>Cut off or corrupted.</summary>
    Damaged,
}
