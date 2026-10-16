using Sim.Events;
using Sim.Input;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Tests.Events;

/// <summary>Emits a landing for P2 every frame, plus a hit for P1 while P1 holds anything (detail: the bits).</summary>
internal sealed class EventEmittingStage : ITickStage
{
    public void Run(ref MatchState state, in FrameActivity activity)
    {
        state.Events.Emit(state.Frame, FrameEventKind.Landing, 1, IntVector2.Zero, 0);

        InputWord input = state.Inputs[0].FramesAgo(0);
        if (input != InputWord.Neutral)
        {
            state.Events.Emit(state.Frame, FrameEventKind.Hit, 0, new IntVector2(input.Bits, 0), input.Bits);
        }
    }
}
