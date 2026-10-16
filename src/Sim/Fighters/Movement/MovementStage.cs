using Sim.Characters;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Fighters.Movement;

/// <summary>The movement step of the tick: moves each fighter that advances this frame.</summary>
/// <remarks>
/// Each fighter is moved from a copy of the state as the step started, and both results are written together
/// (symmetry rule of <see cref="ITickStage"/>).
/// </remarks>
public sealed class MovementStage : ITickStage
{
    private readonly FighterMovement _movement = new();
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;

    public MovementStage(CharacterData player1, CharacterData player2)
    {
        _player1 = player1;
        _player2 = player2;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        if (activity.Advances(0))
        {
            _movement.Advance(ref player1, _player1);
        }

        if (activity.Advances(1))
        {
            _movement.Advance(ref player2, _player2);
        }

        state.Fighters[0] = player1;
        state.Fighters[1] = player2;
    }
}
