using System;
using System.Collections.Generic;

namespace Netcode.Desync;

/// <summary>What one PC knew when it found a desync: version, the states around it and the inputs (ADR-0005 §5).</summary>
/// <remarks>
/// Both PCs write one; <c>tools/DesyncDiff</c> compares the pair. The state at <see cref="LastAgreedFrame"/> plus
/// the inputs after it are enough to rerun the mismatched frame offline.
/// </remarks>
public sealed class DesyncDump
{
    /// <summary>Layout of this file; readers reject other versions instead of misreading them.</summary>
    public const int CurrentFormatVersion = 1;

    public required int FormatVersion { get; init; }

    public required string GameVersion { get; init; }

    public required string DataHash { get; init; }

    /// <summary>1 or 2: the port this PC played.</summary>
    public required int LocalPlayer { get; init; }

    public required DateTimeOffset WrittenAt { get; init; }

    /// <summary>The newest frame both PCs had the same checksum for; 0 when none had matched yet.</summary>
    public required int LastAgreedFrame { get; init; }

    public required int FirstMismatchFrame { get; init; }

    /// <summary>This PC's checksum of <see cref="FirstMismatchFrame"/>, in hex.</summary>
    public required string LocalChecksum { get; init; }

    /// <summary>The other PC's checksum of <see cref="FirstMismatchFrame"/>, in hex.</summary>
    public required string RemoteChecksum { get; init; }

    /// <summary>Null when the frame was not in the history (no frame agreed yet, or the history is too short).</summary>
    public DumpedState? LastAgreedState { get; init; }

    /// <summary>Null only when the history is too short to still hold the frame.</summary>
    public DumpedState? FirstMismatchState { get; init; }

    /// <summary>The confirmed inputs of every frame still in the history, up to the first mismatched one.</summary>
    public required IReadOnlyList<DumpedInputs> Inputs { get; init; }
}
