using Godot;

namespace FightingGame.Input;

/// <summary>Which physical keys mean which direction (player setting: keybinds, applied before the input word).</summary>
public readonly record struct KeyboardLayout(Key Up, Key Down, Key Left, Key Right)
{
    public static KeyboardLayout Player1Default => new(Key.W, Key.S, Key.A, Key.D);

    public static KeyboardLayout Player2Default => new(Key.Up, Key.Down, Key.Left, Key.Right);
}
