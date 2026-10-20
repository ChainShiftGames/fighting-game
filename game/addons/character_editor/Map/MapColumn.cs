using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>One column of the move map: heading, plain hint, and its entries top to bottom (move ids, or a jump).</summary>
public sealed record MapColumn(string Title, string Hint, IReadOnlyList<string> Entries);
