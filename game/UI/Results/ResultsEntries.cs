using System.Collections.Generic;
using FightingGame.Online;
using FightingGame.UI.Online;
using Netcode.Handshake;

namespace FightingGame.UI.Results;

/// <summary>Which choices the results screen offers after a match, by how it was played.</summary>
/// <remarks>
/// No stage select entry until there is a stage select screen (UI-06); it goes after character select then. No
/// "Save replay" either: every finished match is saved on its own (<c>Main</c>, through <c>ReplayFolder</c>).
/// </remarks>
public static class ResultsEntries
{
    /// <param name="canRematch">Whether the match can be played again (the launcher still has it, or the lobby).</param>
    /// <param name="canReselect">Whether character select is on this mode's route (<c>GameFlow.CanReselect</c>).</param>
    /// <param name="isOnline">Played in a lobby: the rematch goes through it, and leaving it is a choice.</param>
    /// <param name="refusal">Why the pre-match checks refused an online match: no rematch then, it would refuse again.</param>
    public static List<ResultsEntry> For(
        LaunchMode mode, bool canRematch, bool canReselect, bool isOnline, HandshakeRefusal refusal)
    {
        var entries = new List<ResultsEntry>();
        if (isOnline)
        {
            if (OnlineResultsText.CanRematch(refusal))
            {
                entries.Add(ResultsEntry.Rematch);
            }

            entries.Add(ResultsEntry.LeaveLobby);
        }
        else if (canRematch)
        {
            entries.Add(mode == LaunchMode.Replay ? ResultsEntry.WatchAgain : ResultsEntry.Rematch);
        }

        if (canReselect)
        {
            entries.Add(ResultsEntry.CharacterSelect);
        }

        entries.Add(ResultsEntry.MainMenu);
        return entries;
    }
}
