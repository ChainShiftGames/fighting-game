using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>A dummy that holds the same input every frame: nothing (stand), down (crouch) or up (jump).</summary>
public sealed class HeldInputDummy : IDummyBehaviour
{
    private readonly InputWord _held;

    public HeldInputDummy(InputWord held)
    {
        _held = held;
    }

    public InputWord Next(in MatchState state) => _held;
}
