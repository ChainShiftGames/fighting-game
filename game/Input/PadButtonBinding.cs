using Godot;

namespace FightingGame.Input;

/// <summary>One joypad button (d-pad included) and the input word bit it sets.</summary>
public readonly record struct PadButtonBinding(JoyButton Button, ushort Mask);
