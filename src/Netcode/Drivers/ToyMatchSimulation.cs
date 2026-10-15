using Sim.Input;
using Sim.Toy;

namespace Netcode.Drivers;

/// <summary>Lets match drivers run the Spike A toy simulation.</summary>
public sealed class ToyMatchSimulation : IMatchSimulation<ToyState>
{
    private readonly ToySimulation _simulation;

    public ToyMatchSimulation(ToySimulation simulation)
    {
        _simulation = simulation;
    }

    public ToyState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref ToyState state, InputWord player1, InputWord player2) =>
        _simulation.Tick(ref state, player1, player2);
}
