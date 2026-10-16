using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Scenarios.Tests.Runner;

/// <summary>Where scenario files and data live; read from the source tree, so editing a file needs no rebuild.</summary>
public static class ScenarioPaths
{
    public const string FileExtension = ".scenario";

    public static string RepositoryRoot { get; } = ReadRepositoryRoot();

    public static string ScenarioRoot => Path.Combine(RepositoryRoot, "tests", "Scenarios");

    /// <summary>The repository's real data/ folder.</summary>
    public static string RepositoryDataRoot => Path.Combine(RepositoryRoot, "data");

    /// <summary>Characters only scenarios use (until Testman, P1-17).</summary>
    public static string FixtureDataRoot => Path.Combine(ScenarioRoot, "Fixtures", "data");

    /// <summary>Every scenario file, relative to <see cref="ScenarioRoot"/> with '/' separators, in ordinal order.</summary>
    public static IReadOnlyList<string> ScenarioFiles() =>
        Directory.EnumerateFiles(ScenarioRoot, "*" + FileExtension, SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(ScenarioRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => !path.StartsWith("bin/", StringComparison.Ordinal) &&
                !path.StartsWith("obj/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

    public static string FullPath(string scenarioFile) => Path.Combine(ScenarioRoot, scenarioFile);

    private static string ReadRepositoryRoot()
    {
        foreach (AssemblyMetadataAttribute attribute in
            typeof(ScenarioPaths).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key == "RepositoryRoot")
            {
                return attribute.Value!;
            }
        }

        throw new InvalidOperationException("RepositoryRoot metadata is missing from the test assembly.");
    }
}
