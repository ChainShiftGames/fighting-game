using System.IO;
using System.Linq;
using System.Reflection;
using FightingGame.Online;
using Sim.Characters;
using Sim.Data;
using Sim.Data.Sources;

namespace FightingGame.Tests;

/// <summary>The repository's data/ folder and the Sim.Data fixture character the tests load.</summary>
internal static class TestGameData
{
    public static string RepositoryRoot { get; } = typeof(TestGameData).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryRoot").Value!;

    public static string DataRoot => Path.Combine(RepositoryRoot, "data");

    /// <summary>A complete data folder with one character, fixture_man.</summary>
    public static string FixtureDataRoot => Path.Combine(RepositoryRoot, "tests", "Sim.Data.Tests", "Fixtures", "data");

    /// <summary>The shipped data with the fixture character as its only character, as the game loads it for now.</summary>
    public static GameData WithFixtureCharacter { get; } = new GameDataLoader(
        new PlaceholderCharacterSource(
            new DataWithoutCharacters(new DirectoryDataSource(DataRoot)),
            new DirectoryDataSource(FixtureDataRoot))).Load();

    public static CharacterData FixtureMan => WithFixtureCharacter.Character("fixture_man");
}
