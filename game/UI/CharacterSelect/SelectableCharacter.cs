using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.UI.CharacterSelect;

/// <summary>One character on the grid: its id and name from the game data, its look from its select file.</summary>
/// <param name="Description">One line on how it plays; may be empty.</param>
/// <param name="Colours">Its colours as <c>#rrggbb</c>, at least one; the index is what the match and replay record.</param>
public sealed record SelectableCharacter(string Id, string Name, MenuText Description, IReadOnlyList<string> Colours);
