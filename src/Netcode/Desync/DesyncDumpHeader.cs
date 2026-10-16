namespace Netcode.Desync;

/// <summary>Which build and data this PC runs and which port it plays, written at the top of each desync dump.</summary>
/// <param name="GameVersion">The game's version string.</param>
/// <param name="DataHash">The game data hash (ARCHITECTURE.md §7), as the handshake compares it.</param>
/// <param name="LocalIsPlayer1">Whether this PC plays P1.</param>
public sealed record DesyncDumpHeader(string GameVersion, string DataHash, bool LocalIsPlayer1);
