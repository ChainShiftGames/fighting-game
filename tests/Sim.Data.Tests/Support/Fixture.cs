using System;
using System.IO;
using System.Reflection;
using Sim.Data.Sources;

namespace Sim.Data.Tests.Support;

/// <summary>The fixture character in Fixtures/data/ and the repository's real data/ folder.</summary>
public static class Fixture
{
    public const string CharacterId = "fixture_man";
    public const string CharacterFile = "characters/fixture_man/character.json";

    public static string DataRoot => Path.Combine(AppContext.BaseDirectory, "Fixtures", "data");

    public static string RepositoryDataRoot
    {
        get
        {
            foreach (AssemblyMetadataAttribute attribute in
                typeof(Fixture).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
            {
                if (attribute.Key == "RepositoryRoot")
                {
                    return Path.Combine(attribute.Value!, "data");
                }
            }

            throw new InvalidOperationException("RepositoryRoot metadata is missing from the test assembly.");
        }
    }

    public static string MoveFile(string moveId) => $"characters/fixture_man/moves/{moveId}.json";

    /// <summary>A fresh in-memory copy of the fixture data with LF line endings, safe to modify.</summary>
    /// <remarks>Git may check the files out with CRLF on Windows; tests that edit text expect LF.</remarks>
    public static InMemoryDataSource InMemory()
    {
        InMemoryDataSource source = new();
        foreach (string file in Directory.GetFiles(DataRoot, "*.json", SearchOption.AllDirectories))
        {
            string path = Path.GetRelativePath(DataRoot, file).Replace(Path.DirectorySeparatorChar, '/');
            string text = File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal);
            source.SetText(path, text);
        }

        return source;
    }

    public static GameData Load(IDataSource source) => new GameDataLoader(source).Load();
}
