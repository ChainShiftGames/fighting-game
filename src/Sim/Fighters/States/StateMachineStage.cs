using Sim.Characters;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>The move state machines step of the tick: advances each fighter that advances this frame.</summary>
/// <remarks>
/// Each fighter is advanced from a copy of the state as the step started, and both results are written together
/// (symmetry rule of <see cref="ITickStage"/>). With the match's modules, they decide which data cancel windows can
/// be taken (<see cref="CancelPermission"/>); without, every window can. Every fighter's started move category is
/// cleared before the step and its move lock after it, whether or not it advanced
/// (<see cref="FighterState.StartedMoveCategory"/>, <see cref="FighterState.LockedMoveCategories"/>).
/// </remarks>
public sealed class StateMachineStage : ITickStage
{
    private readonly FighterStateMachine _player1Machine;
    private readonly FighterStateMachine _player2Machine;
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly ModuleLineup? _modules;

    /// <summary>Both fighters share one move input, e.g. a stand-in that doesn't depend on the character.</summary>
    public StateMachineStage(IMoveInput input, CharacterData player1, CharacterData player2)
        : this(input, input, player1, player2)
    {
    }

    /// <summary>Each fighter has its own move input, e.g. a <see cref="MotionMoveInput"/> for its character.</summary>
    /// <param name="player1Input">P1's move input.</param>
    /// <param name="player2Input">P2's move input.</param>
    /// <param name="player1">P1's character.</param>
    /// <param name="player2">P2's character.</param>
    /// <param name="modules">The match's modules, which decide on data cancels; none lets every window cancel.</param>
    public StateMachineStage(
        IMoveInput player1Input,
        IMoveInput player2Input,
        CharacterData player1,
        CharacterData player2,
        ModuleLineup? modules = null)
    {
        _modules = modules;
        _player1Machine = new FighterStateMachine(player1Input);
        _player2Machine = new FighterStateMachine(player2Input);
        _player1 = player1;
        _player2 = player2;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        player1.StartedMoveCategory = MoveCategory.None;
        player2.StartedMoveCategory = MoveCategory.None;
        if (activity.Advances(0))
        {
            _player1Machine.Advance(ref player1, in state.Inputs[0], _player1, CancelsFor(ref state, 0));
        }

        if (activity.Advances(1))
        {
            _player2Machine.Advance(ref player2, in state.Inputs[1], _player2, CancelsFor(ref state, 1));
        }

        // A move lock covers one step, whether or not the fighter advanced in it.
        player1.LockedMoveCategories = MoveCategory.None;
        player2.LockedMoveCategories = MoveCategory.None;
        state.Fighters[0] = player1;
        state.Fighters[1] = player2;
    }

    private CancelPermission CancelsFor(ref MatchState state, int fighter) =>
        _modules is null ? CancelPermission.EveryWindow : new CancelPermission(ref state, _modules, fighter);

    /// <summary>Puts both fighters in their idle move, as at the start of a round.</summary>
    public void EnterIdle(ref MatchState state)
    {
        MoveEntry.EnterBasic(ref state.Fighters[0], FighterAction.Idle, _player1);
        MoveEntry.EnterBasic(ref state.Fighters[1], FighterAction.Idle, _player2);
    }
}
