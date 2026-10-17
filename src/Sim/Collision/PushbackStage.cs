using Sim.Characters;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Collision;

/// <summary>
/// The pushback part of the collision step: each defender with a slide moves its share for this frame away from the
/// attack. Against a wall the part it can't move goes to the attacker, pushed back the other way: the corner rule
/// that keeps pressure from staying point blank forever.
/// </summary>
/// <remarks>
/// Runs before pushboxes and walls are resolved (<see cref="CollisionStage"/>), which then clean up any overlap. The
/// share is the distance left divided by the frames left, rounded up, so the slide ends exactly on its last frame. Only
/// a fighter that advances this frame slides (not in hitstop, a freeze or a slowed-down skip). Both fighters are read
/// from copies of the state as the step started and written together (symmetry rule of <see cref="ITickStage"/>).
/// </remarks>
public sealed class PushbackStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly StageWalls _walls;

    public PushbackStage(CharacterData player1, CharacterData player2, StageWalls walls)
    {
        _player1 = player1;
        _player2 = player2;
        _walls = walls;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        int toPlayer2 = activity.Advances(0) ? Slide(ref player1, _player1) : 0;
        int toPlayer1 = activity.Advances(1) ? Slide(ref player2, _player2) : 0;
        player1.Position = new IntVector2(player1.Position.X + toPlayer1, player1.Position.Y);
        player2.Position = new IntVector2(player2.Position.X + toPlayer2, player2.Position.Y);
        state.Fighters[0] = player1;
        state.Fighters[1] = player2;
    }

    // Moves the fighter its share; returns how far the attacker is pushed instead (signed x), 0 if not at all.
    private int Slide(ref FighterState fighter, CharacterData character)
    {
        ref PushbackSlide slide = ref fighter.Pushback;
        if (slide.FramesLeft <= 0)
        {
            return 0;
        }

        int share = (slide.DistanceLeft + slide.FramesLeft - 1) / slide.FramesLeft;
        slide.DistanceLeft -= share;
        slide.FramesLeft--;
        AxisAlignedBox box = Pushbox.Of(character, in fighter);
        int room = box.IsEmpty ? share : RoomTowardWall(box, slide.Direction);
        int moved = share < room ? share : room;
        fighter.Position = new IntVector2(fighter.Position.X + (slide.Direction * moved), fighter.Position.Y);
        int stopped = share - moved;
        int pushed = slide.PushesAttacker != 0 ? -slide.Direction * stopped : 0;
        if (slide.FramesLeft == 0)
        {
            slide = default;
        }

        return pushed;
    }

    private int RoomTowardWall(AxisAlignedBox box, int direction)
    {
        int room = direction > 0 ? _walls.Right - box.MaxX : box.MinX - _walls.Left;
        return room > 0 ? room : 0;
    }
}
