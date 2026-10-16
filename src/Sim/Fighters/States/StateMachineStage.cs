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
    private readonly FighterStateMachine _player1Machine;
    private readonly FighterStateMachine _player2Machine;
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;

    /// <summary>Both fighters share one move input, e.g. a stand-in that doesn't depend on the character.</summary>
    public StateMachineStage(IMoveInput input, CharacterData player1, CharacterData player2)
        : this(input, input, player1, player2)
    {
    }

    /// <summary>Each fighter has its own move input, e.g. a <see cref="MotionMoveInput"/> for its character.</summary>
    public StateMachineStage(
        IMoveInput player1Input, IMoveInput player2Input, CharacterData player1, CharacterData player2)
    {
        _player1Machine = new FighterStateMachine(player1Input);
        _player2Machine = new FighterStateMachine(player2Input);
        _player1 = player1;
        _player2 = player2;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        if (activity.Advances(0))
        {
            _player1Machine.Advance(ref player1, in state.Inputs[0], _player1);
        }

        if (activity.Advances(1))
        {
            _player2Machine.Advance(ref player2, in state.Inputs[1], _player2);
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
