using Sim.Characters;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Collision;

/// <summary>
/// The pushbox and wall part of the collision step: fighters pushed apart, kept between the walls, wall bounces,
/// turned.
/// </summary>
/// <remarks>
/// Runs after movement, so it corrects where movement put the fighters. Pushing applies to every fighter, frozen or
/// not, so boxes never stay overlapped; only fighters that advance this frame turn around. Both fighters are read
/// from copies of the state as the step started and written together (symmetry rule of <see cref="ITickStage"/>).
/// </remarks>
public sealed class CollisionStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly StageWalls _walls;

    public CollisionStage(CharacterData player1, CharacterData player2, StageWalls walls)
    {
        _player1 = player1;
        _player2 = player2;
        _walls = walls;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        PushResolver.Resolve(
            ref player1, Pushbox.Of(_player1, in player1), ref player2, Pushbox.Of(_player2, in player2), in _walls);

        WallBounces.Apply(ref player1, _player1, in _walls);
        WallBounces.Apply(ref player2, _player2, in _walls);

        FighterState pushed1 = player1;
        if (activity.Advances(0))
        {
            SideSwitch.Apply(ref player1, in player2);
        }

        if (activity.Advances(1))
        {
            SideSwitch.Apply(ref player2, in pushed1);
        }

        state.Fighters[0] = player1;
        state.Fighters[1] = player2;
    }
}
