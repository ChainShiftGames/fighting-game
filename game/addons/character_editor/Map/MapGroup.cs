using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>A category some move chains into as a whole (e.g. any special move), drawn as a box around its moves.</summary>
public sealed record MapGroup(string Category, IReadOnlyList<string> Moves);
