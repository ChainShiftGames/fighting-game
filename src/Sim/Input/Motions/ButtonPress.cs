using Sim.State;

namespace Sim.Input.Motions;

/// <summary>Finds button presses in an input history: frames on which a button combination became fully held.</summary>
/// <remarks>
/// Starting an action reads only unused presses (<see cref="IsUnusedPressAt"/>, ADR-0033): a press that already
/// started one doesn't start another. Checks that start nothing, such as a throw tech, may read every press.
/// </remarks>
public static class ButtonPress
{
    /// <summary>Pressed on the frame <paramref name="framesAgo"/> back, whether or not the press was used.</summary>
    public static bool IsPressedAt(in InputHistory history, int buttonMask, int framesAgo)
    {
        ushort mask = (ushort)buttonMask;
        return history.FramesAgo(framesAgo).IsHeld(mask) && !history.FramesAgo(framesAgo + 1).IsHeld(mask);
    }

    /// <summary>Pressed on the frame <paramref name="framesAgo"/> back, and that press hasn't started an action.</summary>
    public static bool IsUnusedPressAt(in InputHistory history, int buttonMask, int framesAgo) =>
        !history.IsUsed(framesAgo) && IsPressedAt(in history, buttonMask, framesAgo);

    /// <summary>Pressed on any of the fighter's newest <paramref name="buffer"/> + 1 own frames.</summary>
    /// <remarks>
    /// Frames the fighter skipped (hitstop, slowdown) don't age a press
    /// (<see cref="InputHistory.OldestFrameWithin"/>): with a buffer of 0, a press made since the fighter last
    /// advanced still counts.
    /// </remarks>
    public static bool IsPressedWithin(in InputHistory history, int buttonMask, int buffer)
    {
        int oldest = history.OldestFrameWithin(0, buffer);
        for (int framesAgo = 0; framesAgo <= oldest; framesAgo++)
        {
            if (IsPressedAt(in history, buttonMask, framesAgo))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Like <see cref="IsPressedWithin"/>, counting only presses that haven't started an action
    /// (<see cref="IsUnusedPressAt"/>).
    /// </summary>
    public static bool IsUnusedPressWithin(in InputHistory history, int buttonMask, int buffer)
    {
        int oldest = history.OldestFrameWithin(0, buffer);
        for (int framesAgo = 0; framesAgo <= oldest && !history.IsUsed(framesAgo); framesAgo++)
        {
            if (IsPressedAt(in history, buttonMask, framesAgo))
            {
                return true;
            }
        }

        return false;
    }
}
