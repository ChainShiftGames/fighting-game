namespace Sim.Events;

/// <summary>Identifies an event across re-simulations of its frame, so presentation plays it once (ADR-0005).</summary>
/// <remarks>
/// Built only from what makes it "the same event": frame, kind, fighter and its <see cref="Occurrence"/> among
/// events of that kind and fighter in that frame. Position and detail are left out on purpose: a hit that rollback
/// re-simulates a few subpixels away is still the same hit, so its spark keeps playing instead of restarting.
/// </remarks>
/// <param name="Occurrence">0 for the first event of this kind and fighter in the frame, 1 for the second, …</param>
public readonly record struct FrameEventId(int Frame, FrameEventKind Kind, int Fighter, int Occurrence);
