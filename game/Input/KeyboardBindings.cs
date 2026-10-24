namespace FightingGame.Input;

/// <summary>A keyboard layout as the reader uses it: keys and the input word bits they set (player setting).</summary>
public sealed record KeyboardBindings(KeyBinding[] Keys);
