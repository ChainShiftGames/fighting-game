using Godot;

namespace FightingGame.Input;

/// <summary>
/// One analog trigger used as a button, and the input word bit it sets. Pads and many leverless controllers in
/// XInput mode report their seventh and eighth buttons as triggers.
/// </summary>
public readonly record struct PadTriggerBinding(JoyAxis Trigger, ushort Mask);
