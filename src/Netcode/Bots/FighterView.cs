using Sim.Characters;
using Sim.Fighters.States;

namespace Netcode.Bots;

/// <summary>One fighter as a player sees it on screen: where it is, its facing, its health and its pose.</summary>
/// <remarks>
/// Everything here can be read off the screen (or, for the attack's guard and phase, learned from the animation as
/// players learn frame data). Hidden internals (stun frames left, input history, cancel windows, the match's random
/// state) are deliberately missing, so a bot can't play on knowledge a human couldn't have.
/// </remarks>
/// <param name="X">Horizontal position in subpixels.</param>
/// <param name="Y">Height above the floor in subpixels.</param>
/// <param name="Facing">+1 facing right, -1 facing left.</param>
/// <param name="Health">Health left, as the health bar shows it.</param>
/// <param name="IsAirborne">Off the ground.</param>
/// <param name="Pose">What the animation shows it doing.</param>
/// <param name="AttackPhase">Startup, active or recovery while <see cref="Pose"/> is attacking; otherwise None.</param>
/// <param name="AttackGuard">How the attack must be blocked (its first hit); Mid when not attacking.</param>
public readonly record struct FighterView(
    int X,
    int Y,
    int Facing,
    int Health,
    bool IsAirborne,
    FighterPose Pose,
    MovePhase AttackPhase,
    Guard AttackGuard)
{
    /// <summary>Standing, crouching or walking on the ground: free to attack, move or block.</summary>
    public bool IsFreeOnGround => !IsAirborne && Pose is FighterPose.Standing or FighterPose.Crouching
        or FighterPose.Walking;

    /// <summary>In the startup or active frames of an attack: about to hit, or hitting.</summary>
    public bool IsThreatening => Pose == FighterPose.Attacking && AttackPhase is MovePhase.Startup or MovePhase.Active;

    /// <summary>Stuck in an attack's recovery: the window to punish it.</summary>
    public bool IsRecovering => Pose == FighterPose.Attacking && AttackPhase == MovePhase.Recovery;
}
