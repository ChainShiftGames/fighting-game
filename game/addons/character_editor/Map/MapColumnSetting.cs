using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>One column of normals on the move map: its heading, a plain hint under it, and the buttons it holds.</summary>
public sealed record MapColumnSetting(string Title, string Hint, IReadOnlyList<string> Buttons);
