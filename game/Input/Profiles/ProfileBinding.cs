namespace FightingGame.Input.Profiles;

/// <summary>
/// One control bound to one input: a game input (<c>up</c>, <c>left</c>, a button id from the button set) or a
/// menu input (<see cref="MenuInputs"/>).
/// </summary>
public readonly record struct ProfileBinding(string Input, BoundControl Control);
