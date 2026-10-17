namespace Sim.Characters;

/// <summary>A move a character names for a job a module needs (ADR-0023), e.g. the move a burst cancels into.</summary>
/// <param name="Name">The role's data name (<c>character.json</c> <c>moveRoles</c> key), as a module asks for it.</param>
/// <param name="Move">The move's index in the character's move list.</param>
public readonly record struct MoveRole(string Name, int Move);
