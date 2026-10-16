using Sim.Characters;
using Sim.Input;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>
/// Minimal move input: the button pressed this frame plus a single-direction motion (5 standing, 2 crouching, 6, 4…).
/// </summary>
/// <remarks>
/// Stand-in until the motion parser and button buffer (P1-06) exist: multi-direction motions (236…) and dashes are
/// never triggered here. Motion 5 accepts any standing direction (4, 5, 6) and 2 any crouching one (1, 2, 3), as in
/// numpad notation for normals.
/// </remarks>
public sealed class ButtonPressMoveInput : IMoveInput
{
    public bool IsTriggered(in InputHistory history, in MoveData move, int facing)
    {
        InputWord now = history.FramesAgo(0);
        InputWord previous = history.FramesAgo(1);
        bool isPressed = move.ButtonMask != 0 && (now.Bits & move.ButtonMask) != 0 &&
            (previous.Bits & move.ButtonMask) == 0;
        return isPressed && MatchesDirection(move.Motion, NumpadDirection.Of(now, facing));
    }

    public bool IsDashTriggered(in InputHistory history, int facing) => false;

    public bool IsBackdashTriggered(in InputHistory history, int facing) => false;

    private static bool MatchesDirection(int motion, int direction) => motion switch
    {
        NumpadDirection.Neutral => !NumpadDirection.IsDown(direction) && !NumpadDirection.IsUp(direction),
        NumpadDirection.Down => NumpadDirection.IsDown(direction),
        >= 1 and <= 9 => motion == direction,
        _ => false,
    };
}
