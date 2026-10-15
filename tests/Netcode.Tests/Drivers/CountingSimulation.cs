using Netcode.Drivers;
using Sim.Input;
using Sim.Toy;

namespace Netcode.Tests.Drivers;

/// <summary>The toy simulation, counting how often a frame is run, to prove resimulation actually happens.</summary>
internal sealed class CountingSimulation : IMatchSimulation<ToyState>
{
    private readonly ToyMatchSimulation _simulation = new(new ToySimulation(ToyRules.Default));

    public int TickCalls { get; private set; }

    public ToyState CreateInitialState() => _simulation.CreateInitialState();

    public void Tick(ref ToyState state, InputWord player1, InputWord player2)
    {
        _simulation.Tick(ref state, player1, player2);
        TickCalls++;
    }
}
