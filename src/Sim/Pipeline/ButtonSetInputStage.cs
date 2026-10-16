using Sim.Input;
using Sim.State;

namespace Sim.Pipeline;

/// <summary>The pipeline's input step backed by <see cref="InputStep"/>: button set from data, then SOCD.</summary>
public sealed class ButtonSetInputStage : IInputStage
{
    private readonly InputStep _step;

    public ButtonSetInputStage(ButtonSet buttons)
    {
        _step = new InputStep(buttons);
    }

    public void Run(ref MatchState state, InputWord player1, InputWord player2) =>
        _step.Run(ref state.Inputs, player1, player2);
}
