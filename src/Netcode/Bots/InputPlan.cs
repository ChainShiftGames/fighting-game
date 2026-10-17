using System;
using Sim.Input;

namespace Netcode.Bots;

/// <summary>
/// The input words a bot has committed to for the next few frames: a motion, a jump, a dash. Like a player
/// mid-motion, it plays them out before deciding again.
/// </summary>
/// <remarks>A fixed buffer allocated once; writing past its end drops the extra frames.</remarks>
public sealed class InputPlan
{
    /// <summary>Room for the longest plan a bot makes (a six-step motion plus its button and release).</summary>
    public const int Capacity = 32;

    private readonly ushort[] _words = new ushort[Capacity];
    private int _count;
    private int _next;

    public bool IsEmpty => _next == _count;

    public void Clear()
    {
        _count = 0;
        _next = 0;
    }

    /// <summary>Holds <paramref name="bits"/> for <paramref name="frames"/> frames after what is planned.</summary>
    public void Hold(ushort bits, int frames)
    {
        for (int frame = 0; frame < frames && _count < Capacity; frame++)
        {
            _words[_count++] = bits;
        }
    }

    /// <summary>Plans a move's input: each motion step for a frame, the buttons with the last, a release.</summary>
    /// <param name="motion">Numpad notation read as a number (236, 5…).</param>
    /// <param name="buttonMask">The buttons to press with the last step; 0 for a motion alone (a dash).</param>
    /// <param name="facing">+1 facing right, -1 facing left: which way 6 is.</param>
    /// <remarks>
    /// The same direction twice in a row (66, 22) gets a neutral frame between, so it reads as two presses.
    /// </remarks>
    public void Motion(int motion, int buttonMask, int facing)
    {
        Span<int> steps = stackalloc int[10];
        int stepCount = 0;
        for (int rest = motion; rest > 0 && stepCount < steps.Length; rest /= 10)
        {
            steps[stepCount++] = rest % 10;
        }

        for (int step = stepCount - 1; step >= 0; step--)
        {
            ushort direction = NumpadBits(steps[step], facing);
            if (step < stepCount - 1 && steps[step] == steps[step + 1])
            {
                Hold(0, 1);
            }

            Hold(step == 0 ? (ushort)(direction | buttonMask) : direction, 1);
        }

        // Let go, so the next press of the same button is a fresh press. A motion alone (a dash) keeps its direction
        // for whatever is planned next.
        if (buttonMask != 0)
        {
            Hold(0, 1);
        }
    }

    /// <summary>The next planned word, if any.</summary>
    public bool TryTake(out InputWord input)
    {
        if (IsEmpty)
        {
            input = InputWord.Neutral;
            return false;
        }

        input = new InputWord(_words[_next++]);
        if (IsEmpty)
        {
            Clear();
        }

        return true;
    }

    /// <summary>A numpad direction (6 forward, 4 back, 8 up, 2 down…) as input word bits for this facing.</summary>
    public static ushort NumpadBits(int direction, int facing)
    {
        ushort forward = facing >= 0 ? InputWord.Right : InputWord.Left;
        ushort back = facing >= 0 ? InputWord.Left : InputWord.Right;
        int column = (direction - 1) % 3;
        int row = (direction - 1) / 3;
        ushort horizontal = column switch
        {
            0 => back,
            2 => forward,
            _ => 0,
        };
        ushort vertical = row switch
        {
            0 => InputWord.Down,
            2 => InputWord.Up,
            _ => 0,
        };
        return (ushort)(horizontal | vertical);
    }
}
