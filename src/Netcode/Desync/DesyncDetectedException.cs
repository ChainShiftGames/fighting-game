using System;

namespace Netcode.Desync;

/// <summary>A match driver found that two runs of the same frame disagree: the game logic is not deterministic.</summary>
/// <remarks>Never caught to carry on: a desync is always reported, never hidden (ARCHITECTURE.md §8).</remarks>
public sealed class DesyncDetectedException : Exception
{
    public DesyncDetectedException(string message)
        : base(message)
    {
    }

    public DesyncDetectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <param name="dumpPath">The desync dump this PC wrote.</param>
    public DesyncDetectedException(string message, string dumpPath)
        : base(message)
    {
        DumpPath = dumpPath;
    }

    /// <summary>The desync dump this PC wrote; null when the desync came without one (e.g. SyncTest).</summary>
    public string? DumpPath { get; }
}
