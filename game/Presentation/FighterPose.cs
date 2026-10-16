using System.Globalization;
using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace FightingGame.Presentation;

/// <summary>
/// What the placeholder art shows for a fighter on this logic frame: its action, move, animation frame and frame
/// data phase.
/// </summary>
/// <remarks>
/// The animation frame is the move frame from the game state, never elapsed time (ARCHITECTURE.md §9), so a
/// paused, stepped or rolled-back match always shows the frame the logic is on.
/// </remarks>
public readonly record struct FighterPose(FighterAction Action, string MoveId, int Frame, int TotalFrames, MovePhase Phase)
{
    public static FighterPose Of(CharacterData character, in FighterState fighter)
    {
        MoveData move = character.Moves[fighter.Move];
        return new FighterPose(
            fighter.Action,
            character.MoveIds[fighter.Move],
            fighter.MoveFrame,
            move.TotalFrames,
            MovePhases.Of(in move, fighter.MoveFrame));
    }

    /// <summary>E.g. <c>5a 6/16</c> for an attack, <c>WalkForward: idle 12/60</c> for a basic action.</summary>
    public string Label => Action == FighterAction.Attack
        ? string.Create(CultureInfo.InvariantCulture, $"{MoveId} {Frame}/{TotalFrames}")
        : string.Create(CultureInfo.InvariantCulture, $"{Action}: {MoveId} {Frame}/{TotalFrames}");
}
