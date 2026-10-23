using System;

namespace FightingGame.UI.Framework;

/// <summary>What a menu press means, whatever device it came from (<see cref="MenuBindings"/>).</summary>
/// <remarks>Flags: one control can mean two actions, e.g. the keyboard's Escape is both back and start.</remarks>
[Flags]
public enum MenuAction
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Confirm = 1 << 4,
    Back = 1 << 5,
    TabLeft = 1 << 6,
    TabRight = 1 << 7,

    /// <summary>Opens or closes the pause menu during a match.</summary>
    Start = 1 << 8,

    /// <summary>The actions that repeat while held, so long lists scroll.</summary>
    Directions = Up | Down | Left | Right,
}
