using System.IO;
using System.Linq;
using System.Reflection;
using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Rules;

namespace DesyncDiff.Tests;

/// <summary>Testman against Testman, composed from the real <c>data/</c> as the game builds it.</summary>
internal static class TestmanMatch
{
    private static readonly GameData _data = new GameDataLoader(new DirectoryDataSource(DataRoot())).Load();

    /// <summary>The game data the match is composed from, for its handshake identity.</summary>
    public static GameData Data => _data;

    public static MatchSimulation Create()
    {
        CharacterData testman = _data.Character("testman");
        MatchRules rules = _data.CreateMatchRules();
        return new MatchSimulation(testman, testman, _data.Buttons, _data.Motions.Motions, _data.Motions.Movement, in rules);
    }

    private static string DataRoot() => Path.Combine(
        typeof(TestmanMatch).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryRoot").Value!,
        "data");
}
