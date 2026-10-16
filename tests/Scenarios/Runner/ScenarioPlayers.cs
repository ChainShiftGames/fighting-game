namespace Scenarios.Tests.Runner;

/// <summary>How scenarios name the players: <c>p1</c> is player index 0, <c>p2</c> is index 1.</summary>
public static class ScenarioPlayers
{
    /// <summary>The player of a statement about the whole match rather than one fighter.</summary>
    public const int None = -1;

    public static bool TryParse(string text, out int player)
    {
        player = text switch
        {
            "p1" => 0,
            "p2" => 1,
            _ => None,
        };
        return player != None;
    }

    public static string Label(int player) => $"p{player + 1}";
}
