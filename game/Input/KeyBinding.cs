using Godot;

namespace FightingGame.Input;

/// <summary>One physical key and the input word bit it sets.</summary>
public readonly record struct KeyBinding(Key Key, ushort Mask);
