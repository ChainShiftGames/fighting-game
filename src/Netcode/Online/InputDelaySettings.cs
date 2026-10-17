using System;

namespace Netcode.Online;

/// <summary>How this PC picks its input delay: automatically from the ping, or as the player set it (ADR-0005 §4).</summary>
/// <remarks>
/// Input delay is local to each PC: Backdash tags every input with the frame it applies to, so the two PCs may use
/// different delays, and change them, without changing game results.
/// </remarks>
public sealed record InputDelaySettings
{
    /// <summary>Lowest delay a player can choose.</summary>
    public const int MinOverrideFrames = 0;

    /// <summary>Highest delay a player can choose.</summary>
    public const int MaxOverrideFrames = 4;

    private readonly int? _playerOverrideFrames;

    /// <summary>How the automatic delay follows the ping. Developer settings, from <c>data/netcode/online.json</c>.</summary>
    public required InputDelayTuning Automatic { get; init; }

    /// <summary>
    /// A fixed delay the player chose instead of the automatic one, <see cref="MinOverrideFrames"/> to
    /// <see cref="MaxOverrideFrames"/>; null for automatic. Player setting (options menu).
    /// </summary>
    public int? PlayerOverrideFrames
    {
        get => _playerOverrideFrames;
        init => _playerOverrideFrames = value is null or (>= MinOverrideFrames and <= MaxOverrideFrames)
            ? value
            : throw new ArgumentOutOfRangeException(
                nameof(PlayerOverrideFrames), value, $"Must be {MinOverrideFrames} to {MaxOverrideFrames} frames, or null.");
    }

    /// <summary>The delay the match starts with: the player's choice, else the highest automatic delay.</summary>
    /// <remarks>
    /// No ping is known yet, and a running match can lower its delay but not raise it (<see cref="IInputDelayControl"/>).
    /// </remarks>
    public int StartingFrames => PlayerOverrideFrames ?? Automatic.MaximumFrames;
}
