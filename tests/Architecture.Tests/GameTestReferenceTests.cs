using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// ADR-0016: of everything in tests/ and tools/, only FightingGame.Tests may reference the game (ARCHITECTURE.md §2).
/// </summary>
public sealed class GameTestReferenceTests
{
    private const string GameProject = "FightingGame";
    private const string GameTestProject = "FightingGame.Tests";

    [Fact]
    public void OnlyTheGameTestsReferenceTheGame()
    {
        List<string> referencingGame = ProjectFiles("tests", "tools")
            .Where(project => ProjectReferencesOf(project).Contains(GameProject, StringComparer.Ordinal))
            .Select(project => Path.GetFileNameWithoutExtension(project))
            .Where(name => name != GameTestProject)
            .ToList();

        Assert.True(
            referencingGame.Count == 0,
            $"{string.Join(", ", referencingGame)} reference game/, which ARCHITECTURE.md §2 allows only for " +
            $"{GameTestProject} (ADR-0016).");
    }

    private static IEnumerable<string> ProjectFiles(params string[] topLevelFolders) =>
        topLevelFolders
            .Select(folder => Path.Combine(ArchitectureAssemblies.RepositoryRoot, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is "bin" or "obj"));

    // Names of the projects it references, from the project file itself: a test project has no compiled reference
    // to a project whose types it doesn't use, but the rule is about the dependency, used or not.
    private static IEnumerable<string> ProjectReferencesOf(string projectFile) =>
        XDocument.Load(projectFile).Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .OfType<string>()
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar)));
}
