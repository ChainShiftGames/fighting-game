using System;
using Netcode.Replays;

namespace FightingGame.UI.Online;

/// <summary>One saved replay as the replay browser lists it: when, who played whom in which colour, or why it can't play.</summary>
/// <param name="SavedAt">From the file name; null when the name doesn't say.</param>
/// <param name="Player1">P1's character name; empty for a replay this build can't read.</param>
/// <param name="Colour1">P1's colour index (0 is the first).</param>
/// <param name="Rejection">Why this build can't play it (ADR-0018); null when it can.</param>
public sealed record ReplayEntry(
    string Path,
    DateTime? SavedAt,
    string Player1,
    byte Colour1,
    string Player2,
    byte Colour2,
    ReplayRejection? Rejection)
{
    public bool IsPlayable => Rejection is null;
}
