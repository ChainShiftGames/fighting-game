using Sim.Fighters.States;

namespace FightingGame.Presentation.World;

/// <summary>
/// Counts a hit or block reaction's frames from the hit that started it, by the fighter's own frames: 1 on the frame
/// the hit or block lands, held while hitstop or a freeze holds the fighter (ADR-0028 §4), and back to 1 on every new
/// hit of a combo, though the move stays the same.
/// </summary>
/// <remarks>
/// The reaction's own move frame can't count it: basic moves loop, and stun moves may be shorter than the stun
/// (testman's are one frame long, so their frame is always 1). Its stun can: every hit or block sets it, and it counts
/// down only on frames the fighter advances. Air hitstun lasts until landing however much
/// stun is left (without air recovery its stun doesn't count down), so it counts match frames from the end of its
/// hitstop instead; a freeze during a juggle doesn't hold it, a cosmetic loss. A new hit shows as the stun or the
/// hitstop going up; a hit that raises neither (no hitstop and less stun than is left) carries the reaction on. Logic
/// frames only, never elapsed time (ARCHITECTURE.md §9): after a rollback or a rewound replay it counts from there.
/// </remarks>
public sealed class HitReactionClock
{
    private string? _move;
    private int _lastMatchFrame;
    private int _lastStun;
    private int _lastHitstop;
    private int _stunAtHit;
    private int _hitstopEnd;
    private bool _wasReacting;

    /// <summary>
    /// The pose last observed is a new hit or block landing while the fighter was already reacting to one (a gatling,
    /// a juggle): the model fades into it from the reaction shown, where a first hit snaps. Never after a rollback.
    /// </summary>
    public bool IsReHit { get; private set; }

    /// <summary>Sees the pose shown this frame; call it every frame, in a reaction or not, so a new one is noticed.</summary>
    public void Observe(in FighterPose pose)
    {
        bool isReacting = HitReactionActions.Includes(pose.Action);
        bool isNewHit = pose.MoveId != _move || pose.MatchFrame < _lastMatchFrame ||
            pose.StunFramesLeft > _lastStun || pose.HitstopFramesLeft > _lastHitstop;
        IsReHit = isNewHit && isReacting && _wasReacting && pose.MatchFrame > _lastMatchFrame;
        if (isNewHit)
        {
            _stunAtHit = pose.StunFramesLeft;
            _hitstopEnd = pose.MatchFrame;
        }

        // Hitstop holds the fighter through the match frame it runs out on; in a freeze it doesn't count, so that moves on.
        if (pose.HitstopFramesLeft > 0)
        {
            _hitstopEnd = pose.MatchFrame + pose.HitstopFramesLeft;
        }

        _move = pose.MoveId;
        _wasReacting = isReacting;
        _lastMatchFrame = pose.MatchFrame;
        _lastStun = pose.StunFramesLeft;
        _lastHitstop = pose.HitstopFramesLeft;
    }

    /// <summary>The reaction's frame for the pose last observed: 1 on the frame the hit landed.</summary>
    public int FrameOf(in FighterPose pose)
    {
        int counted = pose.Action == FighterAction.AirHitstun
            ? pose.MatchFrame - _hitstopEnd
            : _stunAtHit - pose.StunFramesLeft;
        return counted > 0 ? counted + 1 : 1;
    }
}
