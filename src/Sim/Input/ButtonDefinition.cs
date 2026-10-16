namespace Sim.Input;

/// <summary>One button in the button set: its name in data and its bit in the <see cref="InputWord"/>.</summary>
public readonly record struct ButtonDefinition(string Name, int Bit);
