using System.Collections.Generic;

namespace Netcode.Desync;

/// <summary>One frame's game state in a desync dump: every field by name, plus the raw bytes to load it again.</summary>
public sealed class DumpedState
{
    public required int Frame { get; init; }

    /// <summary>The state's checksum, in hex.</summary>
    public required string Checksum { get; init; }

    /// <summary>Every integer of the state by name (<see cref="StateFields"/>), in layout order.</summary>
    public required IReadOnlyDictionary<string, long> Fields { get; init; }

    /// <summary>The state's bytes, base64.</summary>
    public required string Bytes { get; init; }
}
