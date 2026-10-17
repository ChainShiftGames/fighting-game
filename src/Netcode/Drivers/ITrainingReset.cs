namespace Netcode.Drivers;

/// <summary>A training input or adjustment that keeps its own progress, which must start over with the match.</summary>
/// <remarks>
/// The training match driver calls it when training restarts or a savestate is loaded, so a dummy's guard, its
/// playback or a refill delay never carries on from a match state it no longer belongs to.
/// </remarks>
public interface ITrainingReset
{
    void Reset();
}
