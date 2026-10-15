using Sim.Input;

namespace Netcode.Drivers;

/// <summary>The game logic a match driver advances: creates the first state and runs one frame on it.</summary>
/// <remarks>
/// <typeparamref name="TState"/> holds the whole match state as plain data (ARCHITECTURE.md §4): drivers snapshot it
/// by copying its bytes and hash those bytes for checksums.
/// </remarks>
public interface IMatchSimulation<TState>
    where TState : unmanaged
{
    TState CreateInitialState();

    void Tick(ref TState state, InputWord player1, InputWord player2);
}
