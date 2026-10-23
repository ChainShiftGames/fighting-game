namespace FightingGame.Input;

/// <summary>What kind of input device a roster entry is.</summary>
public enum InputDeviceKind
{
    /// <summary>The first keyboard layout (WASD by default).</summary>
    KeyboardLeft,

    /// <summary>The second keyboard layout (arrows and number pad by default).</summary>
    KeyboardRight,

    Joypad,
}
