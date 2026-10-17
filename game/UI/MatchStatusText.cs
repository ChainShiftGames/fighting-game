using System.Globalization;
using Netcode.Online;

namespace FightingGame.UI;

/// <summary>
/// The one message shown over a match, most important first: a desync, the connection to the other PC, waiting for it
/// to start, paused. Empty while the match just runs.
/// </summary>
public static class MatchStatusText
{
    public const string OpponentDisconnected = "Opponent disconnected\nThe match ended without a result.";

    /// <param name="desync">Why the match stopped on a desync; empty when it didn't.</param>
    /// <param name="connection">The connection to the other PC; always connected for matches on this PC.</param>
    /// <param name="waiting">What an online match says before its first frame; empty once it runs.</param>
    /// <param name="paused">What a paused local match says; empty when it isn't paused.</param>
    public static string Choose(string desync, ConnectionStatus connection, string waiting, string paused)
    {
        if (desync.Length > 0)
        {
            return $"DESYNC — match stopped\n{desync}";
        }

        string connectionText = Connection(connection);
        if (connectionText.Length > 0)
        {
            return connectionText;
        }

        return waiting.Length > 0 ? waiting : paused;
    }

    /// <summary>What the player sees about the connection: nothing while it's fine.</summary>
    public static string Connection(ConnectionStatus status) => status.State switch
    {
        ConnectionState.Interrupted => string.Create(
            CultureInfo.InvariantCulture,
            $"Connection interrupted\nThe match ends in {status.SecondsUntilDisconnect} s unless the other player is back."),
        ConnectionState.Disconnected => OpponentDisconnected,
        _ => string.Empty,
    };
}
