using System.IO;
using System.Linq;
using System.Reflection;
using Sim.Characters;
using Sim.Data;
using Sim.Data.Sources;

namespace FightingGame.Tests;

/// <summary>The repository's data/ folder, which the game loads, and its character Testman (P1-17).</summary>
internal static class TestGameData
{
    public static string RepositoryRoot { get; } = typeof(TestGameData).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryRoot").Value!;

    public static string DataRoot => Path.Combine(RepositoryRoot, "data");

    /// <summary>All of data/, loaded as local matches load it in editor runs.</summary>
    public static GameData Data { get; } = new GameDataLoader(new DirectoryDataSource(DataRoot)).Load();

    public static CharacterData Testman => Data.Character("testman");
}
