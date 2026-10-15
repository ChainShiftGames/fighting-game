using System;
using Sim.Input;

namespace Netcode.Drivers;

/// <summary>Both players on this PC: every frame runs once with the real inputs, so there is nothing to roll back.</summary>
public sealed class LocalMatchDriver<TState> : IMatchDriver<TState>
    where TState : unmanaged
{
    private readonly IMatchSimulation<TState> _simulation;
    private TState _state;
    private int _currentFrame;

    public LocalMatchDriver(IMatchSimulation<TState> simulation)
    {
        _simulation = simulation;
        _state = simulation.CreateInitialState();
    }

    public int LocalPlayerCount => MatchPlayers.Count;

    public int CurrentFrame => _currentFrame;

    public ref readonly TState State => ref _state;

    public bool TryAdvanceFrame(ReadOnlySpan<InputWord> localInputs)
    {
        MatchPlayers.EnsureInputCount(localInputs, MatchPlayers.Count);
        _simulation.Tick(ref _state, localInputs[0], localInputs[1]);
        _currentFrame++;
        return true;
    }

    // Owns no session or socket.
    public void Dispose()
    {
    }
}
