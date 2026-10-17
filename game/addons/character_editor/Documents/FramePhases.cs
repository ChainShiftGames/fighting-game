using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A move's frame data as phases: startup, active and recovery for an attack, or one loop for a basic move. Frames
/// are 1-based; startup counts the first active frame, so the startup phase itself is <c>Startup - 1</c> frames.
/// </summary>
public sealed record FramePhases
{
    private FramePhases(bool isBasic, int startup, int active, int recovery, int duration)
    {
        IsBasic = isBasic;
        Startup = startup;
        Active = active;
        Recovery = recovery;
        Duration = duration;
    }

    /// <summary>A basic move (idle, walk, hitstun…): <see cref="Duration"/> frames, no attack phases.</summary>
    public bool IsBasic { get; }

    public int Startup { get; }

    public int Active { get; }

    public int Recovery { get; }

    /// <summary>A basic move's length.</summary>
    public int Duration { get; }

    public int TotalFrames => IsBasic ? Duration : Startup - 1 + Active + Recovery;

    public int FirstActiveFrame => Startup;

    public int LastActiveFrame => Startup + Active - 1;

    /// <summary>The phases' lengths in frame order: startup, active, recovery; or the one loop of a basic move.</summary>
    public IReadOnlyList<int> Lengths => IsBasic ? [Duration] : [Startup - 1, Active, Recovery];

    public static FramePhases Attack(int startup, int active, int recovery) => new(false, startup, active, recovery, 0);

    public static FramePhases Basic(int duration) => new(true, 0, 0, 0, duration);

    /// <summary>
    /// The move's phases from its <c>frameData</c>, or its <c>duration</c> when it has no frame data; null when
    /// neither holds whole numbers (validation reports it).
    /// </summary>
    public static FramePhases? Of(JsonObject move)
    {
        if (move[MoveFields.FrameData] is JsonObject frameData)
        {
            int? startup = JsonNodeValue.Int(frameData[MoveFields.Startup]);
            int? active = JsonNodeValue.Int(frameData[MoveFields.Active]);
            int? recovery = JsonNodeValue.Int(frameData[MoveFields.Recovery]);
            return startup is int s && active is int a && recovery is int r ? Attack(s, a, r) : null;
        }

        return JsonNodeValue.Int(move[MoveFields.Duration]) is int duration ? Basic(duration) : null;
    }
}
