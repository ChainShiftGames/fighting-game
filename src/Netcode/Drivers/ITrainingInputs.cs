using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Both fighters' input words for one training frame, from the training player's input and the match so far.</summary>
/// <remarks>
/// This is where the dummy plays: a stance, guarding, the player taking it over to record, or a recording played
/// back.
/// </remarks>
public interface ITrainingInputs<TState>
    where TState : unmanaged
{
    /// <param name="state">The state before the frame: what the dummy reacts to.</param>
    /// <param name="player">The training player's input word for this frame.</param>
    FrameInputs Next(in TState state, InputWord player);
}
