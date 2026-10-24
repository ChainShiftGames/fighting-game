using System.IO;
using System.Text.Json.Nodes;

namespace FightingGame.Tests.Addons.CharacterEditor;

/// <summary>
/// Move files frozen for the editor's own tests, so tuning Testman in <c>data/</c> can't move the frames and boxes
/// they check (ADR-0036). Copies of Testman's moves as of the control panel merge (377b28c).
/// </summary>
internal static class EditorFixtures
{
    /// <summary>
    /// Testman's 5a: startup 5, active 3 (5..7), recovery 8 (8..15); hurt box 1..15 at x -25000, push box 1..15 at
    /// x -20000, hit box 5..7 at x 20000, cancel 5..12.
    /// </summary>
    public static JsonObject Testman5A() => Read("testman_5a.json");

    /// <summary>
    /// Testman's 5b: comes out on frame 7 (hit frames 7..9), 500 damage, hitstun 18, blockstun 14 (+2 / -2); chains
    /// into 5c, 2c, 5d, specials, supers and jumps.
    /// </summary>
    public static JsonObject Testman5B() => Read("testman_5b.json");

    private static JsonObject Read(string file) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(
            TestGameData.RepositoryRoot, "tests", "FightingGame.Tests", "Addons", "CharacterEditor", "Fixtures", file)))!
            .AsObject();
}
