using Sim.State;

namespace Sim.Input.Motions;

/// <summary>Finds button presses in an input history: frames on which a button combination became fully held.</summary>
public static class ButtonPress
{
    public static bool IsPressedAt(in InputHistory history, int buttonMask, int framesAgo)
    {
        ushort mask = (ushort)buttonMask;
        return history.FramesAgo(framesAgo).IsHeld(mask) && !history.FramesAgo(framesAgo + 1).IsHeld(mask);
    }

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
}
