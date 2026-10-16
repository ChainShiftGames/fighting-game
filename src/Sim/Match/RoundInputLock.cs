using Sim.Input;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Match;

/// <summary>The input step, with both players' input replaced by neutral while no round is being fought.</summary>
/// <remarks>
/// From the frame after a KO or time-out until the next round starts, and for good once the match is over, nobody
/// controls their fighter, so a decided round can't be changed or added to. Fighters still finish what they were
/// doing (fall, land, recover) because the rest of the logic keeps running. The neutral words go through the normal
/// input step, so the input history stays one continuous record.
/// </remarks>
public sealed class RoundInputLock : IInputStage
{
    private readonly IInputStage _input;

    public RoundInputLock(IInputStage input)
    {
        _input = input;
    }

    public void Run(ref MatchState state, InputWord player1, InputWord player2)
    {
        if (state.Round.Phase == RoundPhase.Fighting)
        {
            _input.Run(ref state, player1, player2);
        }
        else
        {
            _input.Run(ref state, InputWord.Neutral, InputWord.Neutral);
        }
    }
}
