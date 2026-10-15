using System;

namespace Netcode.Desync;

/// <summary>A match driver found that two runs of the same frame disagree: the game logic is not deterministic.</summary>
/// <remarks>Never caught to carry on: a desync is always reported, never hidden (ARCHITECTURE.md §8).</remarks>
public sealed class DesyncDetectedException : Exception
{
    public DesyncDetectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
