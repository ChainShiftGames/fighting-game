using Sim.Characters;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>The move state machines step of the tick: advances each fighter that advances this frame.</summary>
/// <remarks>
/// Each fighter is advanced from a copy of the state as the step started, and both results are written together
/// (symmetry rule of <see cref="ITickStage"/>).
/// </remarks>
public sealed class StateMachineStage : ITickStage
{
    private readonly FighterStateMachine _machine;
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;

    public StateMachineStage(IMoveInput input, CharacterData player1, CharacterData player2)
    {
        _machine = new FighterStateMachine(input);
        _player1 = player1;
        _player2 = player2;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        if (activity.Advances(0))
        {
            _machine.Advance(ref player1, in state.Inputs[0], _player1);
        }

        if (activity.Advances(1))
        {
            _machine.Advance(ref player2, in state.Inputs[1], _player2);
        }

        state.Fighters[0] = player1;
        state.Fighters[1] = player2;
    }

    /// <summary>Puts both fighters in their idle move, as at the start of a round.</summary>
    public void EnterIdle(ref MatchState state)
    {
        MoveEntry.EnterBasic(ref state.Fighters[0], FighterAction.Idle, _player1);
        MoveEntry.EnterBasic(ref state.Fighters[1], FighterAction.Idle, _player2);
    }
}
