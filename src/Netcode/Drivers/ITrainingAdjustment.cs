namespace Netcode.Drivers;

/// <summary>A training-only change to the state after each logic frame, such as refilling health or holding the timer.</summary>
/// <remarks>
/// Training conveniences live here, in the driver, so the game rules (<c>MatchRules</c>) and the logic stay the same in
/// every match type. Training is never online and never recorded as a replay, so nothing has to reproduce these
/// changes.
/// </remarks>
public interface ITrainingAdjustment<TState>
    where TState : unmanaged
{
    void Apply(ref TState state);
}
