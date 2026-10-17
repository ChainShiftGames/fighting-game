using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>Plays the dummy on its own: its input word for the next frame, from the state before that frame.</summary>
public interface IDummyBehaviour
{
    InputWord Next(in MatchState state);
}
