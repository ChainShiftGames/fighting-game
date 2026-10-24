namespace FightingGame.Input;

/// <summary>A joypad layout as the reader uses it, with its deadzones (player settings, applied before the input word).</summary>
/// <param name="StickDeadzone">Left stick travel (0–1) before it counts as a direction.</param>
/// <param name="TriggerThreshold">Trigger travel (0–1) before a trigger bound as a button counts as held.</param>
/// <param name="LeftStick">Whether the left stick also gives directions, next to the bound d-pad buttons.</param>
public sealed record GamepadBindings(
    PadButtonBinding[] Buttons,
    PadTriggerBinding[] Triggers,
    bool LeftStick,
    float StickDeadzone,
    float TriggerThreshold);
