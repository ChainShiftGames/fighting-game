using Sim.Events;

namespace Sim.State;

/// <summary>The whole match state: everything that changes during a match, and nothing else.</summary>
/// <remarks>
/// Plain, fixed-size data with no references (ARCHITECTURE.md §4): a snapshot is a copy of this struct and the
/// checksum is <see cref="StateChecksum"/> over its bytes. Every nested field must be an integer or a struct of
/// integers laid out without padding; a test checks that. Fields are at most 4 bytes wide, so the struct is 4-byte
/// aligned and has no padding to round up to. Character and move data are read-only during a match and
/// are not in here.
/// </remarks>
public struct MatchState
{
    /// <summary>A 1v1 game: fixed layout, not a setting.</summary>
    public const int PlayerCount = 2;

    public int Frame;
    public RoundState Round;
    public RandomState Random;
    public FreezeState Freeze;
    public FighterStates Fighters;
    public CameraState Camera;
    public InputHistories Inputs;
    public ModuleStates Modules;

    /// <summary>Events of the latest frame for presentation; in the state so they roll back with it (P1-14).</summary>
    public FrameEvents Events;
}
