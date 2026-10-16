using Sim.Characters;
using Sim.Collision;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Camera;

/// <summary>The camera part of the collision step: follows the fighters and holds them within the max distance.</summary>
/// <remarks>
/// Runs after the pushboxes and walls, so it sees where the fighters really stand. The camera centres on the span of
/// both pushboxes (their midpoint when the boxes mirror each other) and stops where the screen would show past a
/// stage wall. When the fighters would span more than the screen, the screen stays where it was last frame and its
/// edges act as walls: a fighter moving out is held at the max distance, and the other one is never pulled along.
/// Pushing applies to every fighter, frozen or not, like the walls. Both fighters are read and written together.
/// </remarks>
public sealed class CameraStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly CameraFraming _framing;

    public CameraStage(CharacterData player1, CharacterData player2, CameraFraming framing)
    {
        _player1 = player1;
        _player2 = player2;
        _framing = framing;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        AxisAlignedBox box1 = Pushbox.Of(_player1, in player1);
        AxisAlignedBox box2 = Pushbox.Of(_player2, in player2);

        if (Right(box1, box2) - Left(box1, box2) > _framing.Width)
        {
            StageWalls screen = _framing.ScreenAt(state.Camera.X);
            PushResolver.Resolve(ref player1, box1, ref player2, box2, in screen);
            box1 = Pushbox.Of(_player1, in player1);
            box2 = Pushbox.Of(_player2, in player2);
        }

        state.Camera.X = _framing.Centre(Left(box1, box2), Right(box1, box2));
        state.Fighters[0] = player1;
        state.Fighters[1] = player2;
    }

    private static int Left(AxisAlignedBox first, AxisAlignedBox second) =>
        first.MinX < second.MinX ? first.MinX : second.MinX;

    private static int Right(AxisAlignedBox first, AxisAlignedBox second) =>
        first.MaxX > second.MaxX ? first.MaxX : second.MaxX;
}
