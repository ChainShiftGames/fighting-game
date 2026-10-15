using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Architecture.Tests;

/// <summary>Build outputs of the runtime assemblies in ARCHITECTURE.md §2, as collected by this project's build.</summary>
internal static class ArchitectureAssemblies
{
    // Written by the CollectArchitectureAssemblies target in Architecture.Tests.csproj.
    private const string AssemblyKey = "ArchitectureAssembly";
    private const string RepositoryRootKey = "RepositoryRoot";

    private static readonly IReadOnlyDictionary<string, string> _pathsByName = ReadMetadata(AssemblyKey)
        .ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.Ordinal);

    public static string RepositoryRoot { get; } = ReadMetadata(RepositoryRootKey).Single();

    public static IEnumerable<string> Names => _pathsByName.Keys.Order(StringComparer.Ordinal);

    public static string PathOf(string assemblyName)
    {
        if (!_pathsByName.TryGetValue(assemblyName, out string? path))
        {
            throw new InvalidOperationException(
                $"'{assemblyName}' is not collected by Architecture.Tests.csproj; add its project there.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Build output of '{assemblyName}' is missing; build the solution.", path);
        }

        return path;
    }

    private static IEnumerable<string> ReadMetadata(string key) =>
        typeof(ArchitectureAssemblies).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Key == key && !string.IsNullOrEmpty(attribute.Value))
            .Select(attribute => attribute.Value!);
}
