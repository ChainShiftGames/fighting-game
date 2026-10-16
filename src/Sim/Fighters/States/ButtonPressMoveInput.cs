using Sim.Characters;
using Sim.Input;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>
/// Minimal move input: the button pressed this frame plus a single-direction motion (5 standing, 2 crouching, 6, 4…).
/// </summary>
/// <remarks>
/// Stand-in until the motion parser and button buffer (P1-06) exist: multi-direction motions (236…) are never
/// triggered here. Motion 5 accepts any standing direction (4, 5, 6) and 2 any crouching one (1, 2, 3), as in numpad
/// notation for normals. Dashes use a dash button instead of 66/44: pressed with back it backdashes, otherwise it
/// dashes. A high jump is up straight after down (2 then 8), a double jump a fresh up press in the air.
/// </remarks>
public sealed class ButtonPressMoveInput : IMoveInput
{
    private readonly ushort _dashButton;

    /// <summary>A stand-in without dashes.</summary>
    public ButtonPressMoveInput()
        : this(dashButton: 0)
    {
    }

    /// <param name="dashButton">Input word mask of the button set's dash button; 0 for no dashes.</param>
    public ButtonPressMoveInput(ushort dashButton)
    {
        _dashButton = dashButton;
    }

    public bool IsTriggered(in InputHistory history, in MoveData move, int facing) =>
        IsPressed(in history, move.ButtonMask) &&
        MatchesDirection(move.Motion, NumpadDirection.Of(history.FramesAgo(0), facing));

    public bool IsDashTriggered(in InputHistory history, int facing) =>
        IsPressed(in history, _dashButton) && !NumpadDirection.IsBack(NumpadDirection.Of(history.FramesAgo(0), facing));

    public bool IsBackdashTriggered(in InputHistory history, int facing) =>
        IsPressed(in history, _dashButton) && NumpadDirection.IsBack(NumpadDirection.Of(history.FramesAgo(0), facing));

    public bool IsHighJumpTriggered(in InputHistory history, int facing) =>
        history.FramesAgo(0).IsHeld(InputWord.Up) && history.FramesAgo(1).IsHeld(InputWord.Down);

    public bool IsAirJumpTriggered(in InputHistory history, int facing) => IsPressed(in history, InputWord.Up);

    // Held this frame but not the one before; a mask of 0 is never pressed.
    private static bool IsPressed(in InputHistory history, int mask) =>
        mask != 0 && (history.FramesAgo(0).Bits & mask) != 0 && (history.FramesAgo(1).Bits & mask) == 0;

    private static bool MatchesDirection(int motion, int direction) => motion switch
    {
        NumpadDirection.Neutral => !NumpadDirection.IsDown(direction) && !NumpadDirection.IsUp(direction),
        NumpadDirection.Down => NumpadDirection.IsDown(direction),
        >= 1 and <= 9 => motion == direction,
        _ => false,
    };
}
