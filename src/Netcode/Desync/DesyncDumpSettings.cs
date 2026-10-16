namespace Netcode.Desync;

/// <summary>Where desync dumps go and how much they hold. Both are developer settings (ARCHITECTURE.md §6).</summary>
/// <remarks>
/// There is deliberately no switch to turn dumps off: a desync is never hidden (ARCHITECTURE.md §8).
/// </remarks>
public sealed record DesyncDumpSettings
{
    /// <summary>Name of the dump folder, inside the game's user data folder.</summary>
    public const string DefaultFolderName = "desyncs";

    /// <summary>
    /// Folder dumps are written to; created when the first dump is written. Developer setting: players don't pick
    /// it, they send its files to the devs.
    /// </summary>
    public required string Folder { get; init; }

    /// <summary>
    /// Frames of states and inputs kept for a dump. Developer setting: it must cover the rollback window plus how
    /// far the other PC's checksums can lag behind, or comparing fails as "history too short".
    /// </summary>
    public required int HistoryFrames { get; init; }
}
