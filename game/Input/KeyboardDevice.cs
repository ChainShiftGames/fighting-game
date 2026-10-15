using Godot;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>Reads the keyboard keys of one <see cref="KeyboardLayout"/>.</summary>
public sealed class KeyboardDevice : IInputDevice
{
    private readonly KeyboardLayout _layout;

    public KeyboardDevice(KeyboardLayout layout)
    {
        _layout = layout;
    }

    // Raw directions only: opposite keys held together stay in the word, SOCD is resolved in the logic.
    public InputWord Read()
    {
        ushort bits = 0;
        bits |= Held(_layout.Up, InputWord.Up);
        bits |= Held(_layout.Down, InputWord.Down);
        bits |= Held(_layout.Left, InputWord.Left);
        bits |= Held(_layout.Right, InputWord.Right);
        return new InputWord(bits);
    }

    // Physical keys, so the layout stays in the same place on AZERTY and other keyboard layouts.
    private static ushort Held(Key key, ushort bit) => Godot.Input.IsPhysicalKeyPressed(key) ? bit : (ushort)0;
}
