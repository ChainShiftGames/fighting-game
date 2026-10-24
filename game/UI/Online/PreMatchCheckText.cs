using System.Collections.Generic;
using FightingGame.UI.Framework;
using Netcode.Handshake;

namespace FightingGame.UI.Online;

/// <summary>What the players see when the pre-match checks refuse a match (ARCHITECTURE.md §8): one line per reason.</summary>
public static class PreMatchCheckText
{
    /// <returns>Empty when nothing was refused.</returns>
    public static List<MenuText> For(HandshakeRefusal refusal)
    {
        var lines = new List<MenuText>();
        if (refusal == HandshakeRefusal.None)
        {
            return lines;
        }

        lines.Add(MenuText.Key("ONLINE_CHECK_FAILED"));
        Add(lines, refusal, HandshakeRefusal.GameVersion, "ONLINE_CHECK_GAME_VERSION");
        Add(lines, refusal, HandshakeRefusal.HandshakeFormat, "ONLINE_CHECK_GAME_VERSION");
        Add(lines, refusal, HandshakeRefusal.GameData, "ONLINE_CHECK_GAME_DATA");
        Add(lines, refusal, HandshakeRefusal.MatchRules, "ONLINE_CHECK_MATCH_RULES");
        lines.Add(MenuText.Key("ONLINE_CHECK_UPDATE"));
        return lines;
    }

    // Another handshake format means another build, worded as a version difference; it is listed once.
    private static void Add(List<MenuText> lines, HandshakeRefusal refusal, HandshakeRefusal reason, string key)
    {
        if ((refusal & reason) != 0 && !lines.Exists(shown => shown.Value == key))
        {
            lines.Add(MenuText.Key(key));
        }
    }
}
