using System.Globalization;
using FightingGame.UI.Framework;
using Netcode.Replays;

namespace FightingGame.UI.Online;

/// <summary>How the replay browser words one replay: date, both characters with their colours, or why it can't play.</summary>
public static class ReplayEntryText
{
    public static MenuText Label(ReplayEntry entry)
    {
        MenuText date = entry.SavedAt is { } savedAt
            ? MenuText.Plain(savedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
            : MenuText.Key("REPLAY_DATE_UNKNOWN");
        if (!entry.IsPlayable)
        {
            return MenuText.Key("REPLAY_ENTRY_UNSUPPORTED", date);
        }

        return MenuText.Key("REPLAY_ENTRY", date, Player(entry.Player1, entry.Colour1), Player(entry.Player2, entry.Colour2));
    }

    /// <summary>Why a replay can't be watched; empty when it can.</summary>
    public static MenuText Reason(ReplayEntry entry) => entry.Rejection switch
    {
        null => MenuText.Empty,
        ReplayRejection.OtherGameVersion or ReplayRejection.OtherFormat => MenuText.Key("REPLAY_OTHER_VERSION"),
        ReplayRejection.OtherGameData => MenuText.Key("REPLAY_OTHER_DATA"),
        _ => MenuText.Key("REPLAY_DAMAGED"),
    };

    private static MenuText Player(string name, byte colour) => MenuText.Key(
        "REPLAY_PLAYER", MenuText.Plain(name), MenuText.Plain((colour + 1).ToString(CultureInfo.InvariantCulture)));
}
