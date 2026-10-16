namespace FightingGame.Input;

/// <summary>Everything one player has bound (player settings, ARCHITECTURE.md §6).</summary>
public sealed record PlayerBindings(KeyboardBindings Keyboard, GamepadBindings Gamepad);
