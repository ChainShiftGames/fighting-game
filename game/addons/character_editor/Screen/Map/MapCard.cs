#if TOOLS
using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Screen.Map;

/// <summary>What one card on the move map shows: plain name, input, a one-line summary and its phases.</summary>
/// <param name="Entry">The move id, or <see cref="CharacterEditor.Map.MoveMapLayout.JumpEntry"/>.</param>
/// <param name="Phases">Phase lengths in frame order (startup, active, recovery; or one loop); empty for none.</param>
/// <param name="HasProblem">Validation reported something in the move's file.</param>
public sealed record MapCard(
    string Entry,
    string Name,
    string Meta,
    string Arrows,
    IReadOnlyList<string> Keycaps,
    IReadOnlyList<int> Phases,
    bool HasProblem,
    string Tooltip);
#endif
