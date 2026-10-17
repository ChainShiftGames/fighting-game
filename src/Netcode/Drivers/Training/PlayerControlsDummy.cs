using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Record mode: the player's input drives the dummy, and the player's own fighter stands still.</summary>
/// <remarks>
/// Only the control swap. Record and playback (P3-32) keep the dummy's words from here in recording slots, by wrapping
/// or replacing this entry in <see cref="DummyModeInputs.Create"/>.
/// </remarks>
public sealed class PlayerControlsDummy : ITrainingInputs<MatchState>
{
    public FrameInputs Next(in MatchState state, InputWord player) => new(InputWord.Neutral, player);
}
