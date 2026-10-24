using System.Globalization;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Online;

/// <summary>The connection to the opponent before a match: bars and ping (<see cref="PingReading"/>), or still measuring.</summary>
public static class ConnectionText
{
    private const char FullBar = '■';
    private const char EmptyBar = '□';

    /// <param name="pingMilliseconds">Steam's estimate; null while unknown.</param>
    public static MenuText For(int? pingMilliseconds)
    {
        if (pingMilliseconds is not { } ping)
        {
            return MenuText.Key("ONLINE_CONNECTION_MEASURING");
        }

        var reading = new PingReading(ping);
        string quality = reading.Quality switch
        {
            PingQuality.Good => "ONLINE_QUALITY_GOOD",
            PingQuality.Fair => "ONLINE_QUALITY_FAIR",
            _ => "ONLINE_QUALITY_POOR",
        };
        return MenuText.Key(
            "ONLINE_CONNECTION",
            MenuText.Plain(Bars(reading.Bars)),
            MenuText.Plain(ping.ToString(CultureInfo.InvariantCulture)),
            MenuText.Key(quality));
    }

    private static string Bars(int filled) =>
        new string(FullBar, filled) + new string(EmptyBar, PingReading.MaxBars - filled);
}
