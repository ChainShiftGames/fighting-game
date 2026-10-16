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

    /// <summary>Pressed on any of the newest <paramref name="buffer"/> + 1 frames.</summary>
    public static bool IsPressedWithin(in InputHistory history, int buttonMask, int buffer)
    {
        for (int framesAgo = 0; framesAgo <= buffer; framesAgo++)
        {
            if (IsPressedAt(in history, buttonMask, framesAgo))
            {
                return true;
            }
        }

        return false;
    }
}
