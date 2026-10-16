using Sim.Input;
using Sim.State;

namespace Sim.Pipeline;

/// <summary>Minimal input step: applies the fixed SOCD rule and records both words in the input histories.</summary>
/// <remarks>Stand-in until P1-05's input step (button set, undefined bits dropped) is plugged in.</remarks>
public sealed class SocdInputStage : IInputStage
{
    public void Run(ref MatchState state, InputWord player1, InputWord player2)
    {
        state.Inputs[0].Record(SocdResolver.Resolve(player1));
        state.Inputs[1].Record(SocdResolver.Resolve(player2));
    }
}
