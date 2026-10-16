using System;
using Sim.Characters;
using Sim.Input;
using Sim.Input.Motions;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Move input through the motion parser and button buffer (P1-06), for one character.</summary>
/// <remarks>
/// Moves go through <see cref="MoveInputMatcher"/> (motion priority among the character's moves). Dashes are their
/// data motions (66/44) or the dash button; a high jump is its data motion, asked on the frame a prejump starts; a
/// double jump is a fresh up press in the air. Built once per fighter at match start; checks never allocate.
/// </remarks>
public sealed class MotionMoveInput : IMoveInput
{
    private readonly MotionParser _parser;
    private readonly MoveInputMatcher _moves;
    private readonly int _dash;
    private readonly int _backdash;
    private readonly int _highJump;
    private readonly int _dashButton;

    /// <exception cref="ArgumentException">
    /// A movement motion isn't in the motion set, or a move uses a motion the set doesn't define.
    /// </exception>
    public MotionMoveInput(MotionParser parser, MovementInputs movement, CharacterData character)
    {
        _parser = parser;
        _moves = new MoveInputMatcher(parser, character);
        _dash = Find(parser.Motions, movement.DashMotion);
        _backdash = Find(parser.Motions, movement.BackdashMotion);
        _highJump = Find(parser.Motions, movement.HighJumpMotion);
        _dashButton = movement.DashButtonMask;
    }

    public bool IsTriggered(in InputHistory history, in MoveData move, int facing) =>
        _moves.IsTriggered(in history, in move, facing);

    public bool IsDashTriggered(in InputHistory history, int facing) =>
        _parser.IsInput(_dash, in history, facing, buttonMask: 0) ||
        (IsDashButtonPressed(in history) && !IsHoldingBack(in history, facing));

    public bool IsBackdashTriggered(in InputHistory history, int facing) =>
        _parser.IsInput(_backdash, in history, facing, buttonMask: 0) ||
        (IsDashButtonPressed(in history) && IsHoldingBack(in history, facing));

    public bool IsHighJumpTriggered(in InputHistory history, int facing) =>
        _parser.IsInput(_highJump, in history, facing, buttonMask: 0);

    public bool IsAirJumpTriggered(in InputHistory history, int facing) =>
        history.FramesAgo(0).IsHeld(InputWord.Up) && !history.FramesAgo(1).IsHeld(InputWord.Up);

    private bool IsDashButtonPressed(in InputHistory history) =>
        _dashButton != 0 && ButtonPress.IsPressedAt(in history, _dashButton, framesAgo: 0);

    private static bool IsHoldingBack(in InputHistory history, int facing) =>
        NumpadDirection.IsBack(NumpadDirection.Of(history.FramesAgo(0), facing));

    private static int Find(MotionSet motions, int notation)
    {
        if (!motions.TryFind(notation, out int motion))
        {
            throw new ArgumentException($"Movement motion {notation} isn't in the motion set.", nameof(motions));
        }

        return motion;
    }
}
