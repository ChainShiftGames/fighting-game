using System.IO;
using ArchUnitNET.Loader;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Architecture.Tests;

/// <summary>The runtime assemblies of ARCHITECTURE.md §2 loaded into ArchUnitNET once for all type-level rules.</summary>
internal static class LoadedArchitecture
{
    // Loaded from the files (Mono.Cecil reads metadata only), so the game and Platform.Steam are inspected
    // without the tests referencing them or loading Godot/Steam into the test process.
    public static ArchitectureModel Model { get; } = Load();

    private static ArchitectureModel Load()
    {
        var loader = new ArchLoader();
        foreach (string assemblyName in RuntimeDependencyGraph.Assemblies)
        {
            string path = ArchitectureAssemblies.PathOf(assemblyName);
            loader.LoadFilteredDirectory(Path.GetDirectoryName(path)!, Path.GetFileName(path));
        }

        return loader.Build();
    }
}
