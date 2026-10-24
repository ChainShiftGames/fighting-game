using System;

namespace Netcode.Handshake;

/// <summary>Why the handshake refused a match: each part that differs between the two PCs.</summary>
/// <remarks>The same reasons as <see cref="VersionHandshake.Refusal"/>, for screens that word them for players.</remarks>
[Flags]
public enum HandshakeRefusal
{
    None = 0,
    GameVersion = 1,
    GameData = 2,
    MatchRules = 4,

    /// <summary>The other PC's hello can't be read: a build with another handshake.</summary>
    HandshakeFormat = 8,
}
