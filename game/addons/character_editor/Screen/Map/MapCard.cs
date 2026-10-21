#if TOOLS
using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>What one card on the move map shows: name, input, a one-line summary, how it's blocked, its phases.</summary>
/// <param name="Entry">The move id, or <see cref="CharacterEditor.Map.MoveMapLayout.JumpEntry"/>.</param>
/// <param name="Guards">Its hits' guards other than mid, each once (<see cref="CharacterEditor.Map.MoveGuards"/>).</param>
/// <param name="Phases">Phase lengths in frame order (startup, active, recovery; or one loop); empty for none.</param>
/// <param name="HasProblem">Validation reported something in the move's file.</param>
public sealed record MapCard(
    string Entry,
    string Name,
    string Meta,
    string Arrows,
    IReadOnlyList<string> Keycaps,
    IReadOnlyList<string> Guards,
    IReadOnlyList<int> Phases,
    bool HasProblem,
    string Tooltip);
#endif
