using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Architecture.Tests;

/// <summary>Finds the projects in the repository folders of ARCHITECTURE.md §2, by assembly name.</summary>
internal static class RepositoryProjects
{
    public static IReadOnlyList<string> In(params string[] topLevelFolders) =>
        topLevelFolders
            .Select(folder => Path.Combine(ArchitectureAssemblies.RepositoryRoot, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories))
            .Where(path => !IsInIgnoredFolder(path))
            // Assembly name = project file name; no project overrides <AssemblyName>.
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    // Build output, Godot's cache (.godot) and vendored addons never hold our projects.
    private static bool IsInIgnoredFolder(string path) =>
        Path.GetRelativePath(ArchitectureAssemblies.RepositoryRoot, path)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.StartsWith('.') || part is "bin" or "obj" or "addons");
}
