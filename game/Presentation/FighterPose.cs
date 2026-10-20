using System.Globalization;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace FightingGame.Presentation;

/// <summary>
/// What the fighter art shows for a fighter on this logic frame: its action, move, animation frame, frame data phase
/// and the move's active frames (first and last; 0 when it has none), which the model retimes its animation by.
/// </summary>
/// <remarks>
/// The animation frame is the move frame from the game state, never elapsed time (ARCHITECTURE.md §9), so a
/// paused, stepped or rolled-back match always shows the frame the logic is on.
/// </remarks>
public readonly record struct FighterPose(
    FighterAction Action,
    string MoveId,
    int Frame,
    int TotalFrames,
    MovePhase Phase,
    int FirstActiveFrame = 0,
    int LastActiveFrame = 0)
{
    /// <summary>
    /// The match frame the pose is shown on: the clock the model counts a move's frames by when the move's own frame
    /// doesn't (a repeated one-frame crouch). 0 when there is no match (the character editor).
    /// </summary>
    public int MatchFrame { get; init; }

    /// <summary>The fighter is KO'd: the model plays its knockout clip from the frame it happened.</summary>
    public bool IsKnockedOut { get; init; }

    /// <summary>
    /// Hitstun or blockstun frames left (<c>FighterState.StunFramesLeft</c>): set anew by every hit or block and
    /// counted down only on frames the fighter advances, so the model counts a reaction by it.
    /// </summary>
    public int StunFramesLeft { get; init; }

    /// <summary>Hitstop frames the fighter still skips (<c>FighterTiming.HitstopFramesLeft</c>); every hit sets it.</summary>
    public int HitstopFramesLeft { get; init; }

    public static FighterPose Of(CharacterData character, in FighterState fighter)
    {
        MoveData move = character.Moves[fighter.Move];
        return new FighterPose(
            fighter.Action,
            character.MoveIds[fighter.Move],
            fighter.MoveFrame,
            move.TotalFrames,
            MovePhases.Of(in move, fighter.MoveFrame),
            move.FirstActiveFrame,
            move.LastActiveFrame)
        {
            IsKnockedOut = KnockOut.IsKnockedOut(in fighter),
            StunFramesLeft = fighter.StunFramesLeft,
            HitstopFramesLeft = fighter.Timing.HitstopFramesLeft,
        };
    }

    /// <summary>E.g. <c>5a 6/16</c> for an attack, <c>WalkForward: idle 12/60</c> for a basic action.</summary>
    public string Label => Action == FighterAction.Attack
        ? string.Create(CultureInfo.InvariantCulture, $"{MoveId} {Frame}/{TotalFrames}")
        : string.Create(CultureInfo.InvariantCulture, $"{Action}: {MoveId} {Frame}/{TotalFrames}");
}
