using Sim.Input;
using Sim.State;

namespace Sim.Tests.Input.Motions;

/// <summary>Builds an input history from numpad directions, oldest frame first, for a given facing.</summary>
internal sealed class InputSequence
{
    public const ushort ButtonA = 1 << 4;
    public const ushort ButtonC = 1 << 6;

    private readonly int _facing;
    private InputHistory _history;

    /// <param name="facing">+1 facing right, -1 facing left: the same numpad digits become mirrored words.</param>
    public InputSequence(int facing = 1)
    {
        _facing = facing;
    }

    public InputHistory History => _history;

    /// <summary>One frame per digit, e.g. "236"; no buttons held.</summary>
    public InputSequence Directions(string numpad)
    {
        foreach (char digit in numpad)
        {
            Hold(digit - '0', 1);
        }

        return this;
    }

    public InputSequence Hold(int direction, int frames, ushort buttons = 0)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            _history.Record(Word(direction, buttons));
        }

        return this;
    }

    /// <summary>One frame of <paramref name="direction"/> with <paramref name="buttons"/> held.</summary>
    public InputSequence Press(int direction, ushort buttons) => Hold(direction, 1, buttons);

    private InputWord Word(int direction, ushort buttons)
    {
        int column = (direction - 1) % 3;
        int row = (direction - 1) / 3;
        ushort forward = _facing >= 0 ? InputWord.Right : InputWord.Left;
        ushort back = _facing >= 0 ? InputWord.Left : InputWord.Right;
        ushort bits = buttons;
        bits |= column == 0 ? back : column == 2 ? forward : (ushort)0;
        bits |= row == 0 ? InputWord.Down : row == 2 ? InputWord.Up : (ushort)0;
        return new InputWord(bits);
    }
}
