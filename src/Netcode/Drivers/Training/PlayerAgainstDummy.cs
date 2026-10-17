using Sim.Input;
using Sim.State;

namespace Netcode.Drivers.Training;

/// <summary>The player plays their fighter; the dummy plays itself with an <see cref="IDummyBehaviour"/>.</summary>
public sealed class PlayerAgainstDummy : ITrainingInputs<MatchState>
{
    private readonly IDummyBehaviour _dummy;

    public PlayerAgainstDummy(IDummyBehaviour dummy)
    {
        _dummy = dummy;
    }

    public FrameInputs Next(in MatchState state, InputWord player) => new(player, _dummy.Next(in state));
}
