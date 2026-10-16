using Sim.Math;

namespace Sim.Events;

/// <summary>One thing that happened in a logic frame that presentation reacts to (hit, block, landing…).</summary>
/// <remarks>
/// Plain integer data stored in the game state (<see cref="FrameEvents"/>). What <see cref="Fighter"/>,
/// <see cref="Position"/> and <see cref="Detail"/> mean depends on the <see cref="Kind"/>; each kind documents it.
/// </remarks>
public readonly struct FrameEvent
{
    /// <summary>Value of <see cref="Fighter"/> for an event that belongs to no fighter (e.g. round start).</summary>
    public const int NoFighter = -1;

    public FrameEvent(int frame, FrameEventKind kind, int fighter, int occurrence, IntVector2 position, int detail)
    {
        Frame = frame;
        Kind = kind;
        Fighter = fighter;
        Occurrence = occurrence;
        Position = position;
        Detail = detail;
    }

    /// <summary>The logic frame (match frame number) the event happened on.</summary>
    public int Frame { get; }

    public FrameEventKind Kind { get; }

    /// <summary>The fighter the event belongs to (0 = P1, 1 = P2), or <see cref="NoFighter"/>.</summary>
    public int Fighter { get; }

    /// <summary>0 for the first event of this kind and fighter in the frame, 1 for the second, …</summary>
    public int Occurrence { get; }

    /// <summary>World position in subpixels, e.g. where a hit spark appears.</summary>
    public IntVector2 Position { get; }

    /// <summary>Kind-specific value, e.g. the attacking move.</summary>
    public int Detail { get; }

    public FrameEventId Id => new(Frame, Kind, Fighter, Occurrence);
}
