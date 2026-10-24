namespace FightingGame.Input;

/// <summary>Everything one player had bound in the old dev bindings file (<see cref="BindingsParser"/>).</summary>
public sealed record PlayerBindings(KeyboardBindings Keyboard, GamepadBindings Gamepad);
