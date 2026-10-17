using System.IO;
using System.Linq;
using System.Reflection;
using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Rules;

namespace Netcode.Tests.Matches;

/// <summary>The real game data from <c>data/</c>, and matches composed from it as the game builds them.</summary>
internal static class TestmanMirror
{
    public const string Character = "testman";

    public static GameData Data { get; } = new GameDataLoader(new DirectoryDataSource(DataRoot())).Load();

    public static string RepositoryRoot() => typeof(TestmanMirror).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryRoot").Value!;

    /// <summary>Testman against Testman with the match rules from data.</summary>
    public static MatchSimulation Create() => Create(Character, Character);

    public static MatchSimulation Create(string player1, string player2)
    {
        CharacterData first = Data.Character(player1);
        CharacterData second = Data.Character(player2);
        MatchRules rules = Data.CreateMatchRules();
        return new MatchSimulation(first, second, Data.Buttons, Data.Motions.Motions, Data.Motions.Movement, in rules, Data.Flags.Names);
    }

    private static string DataRoot() => Path.Combine(RepositoryRoot(), "data");
}
