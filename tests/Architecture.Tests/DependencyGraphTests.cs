using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Architecture.Tests;

/// <summary>ADR-0009: compiled assembly references follow the runtime graph in ARCHITECTURE.md §2.</summary>
public sealed class DependencyGraphTests
{
    public static TheoryData<string> RuntimeAssemblies => [.. RuntimeDependencyGraph.Assemblies];

    [Theory]
    [MemberData(nameof(RuntimeAssemblies))]
    public void CompiledReferencesFollowTheGraph(string assemblyName)
    {
        IReadOnlyList<string> repositoryProjects = RepositoryProjects.In("src", "game", "tests", "tools");

        List<string> forbidden = CompiledReferences.Of(assemblyName)
            .Where(reference => reference != assemblyName && repositoryProjects.Contains(reference))
            .Where(reference => !RuntimeDependencyGraph.CanDependOn(assemblyName, reference))
            .ToList();

        string allowed = string.Join(", ", RuntimeDependencyGraph.AllowedDependenciesOf(assemblyName));
        Assert.True(
            forbidden.Count == 0,
            $"{assemblyName} references {string.Join(", ", forbidden)}, which ARCHITECTURE.md §2 doesn't allow. " +
            $"Allowed: {(allowed.Length == 0 ? "nothing in this repo" : allowed)}.");
    }

    // Without this, a new project in src/ or game/ would never be checked.
    [Fact]
    public void EveryRuntimeProjectIsInTheGraph()
    {
        IReadOnlyList<string> runtimeProjects = RepositoryProjects.In("src", "game");

        List<string> missingFromGraph = runtimeProjects.Where(p => !RuntimeDependencyGraph.Contains(p)).ToList();
        List<string> missingFromRepo = RuntimeDependencyGraph.Assemblies.Where(a => !runtimeProjects.Contains(a)).ToList();

        Assert.True(
            missingFromGraph.Count == 0,
            $"Add {string.Join(", ", missingFromGraph)} to ARCHITECTURE.md §2 and RuntimeDependencyGraph.");
        Assert.True(
            missingFromRepo.Count == 0,
            $"RuntimeDependencyGraph lists {string.Join(", ", missingFromRepo)}, which has no project in src/ or game/.");
    }

    [Fact]
    public void EveryRuntimeAssemblyIsCollectedForInspection()
    {
        Assert.Equal(RuntimeDependencyGraph.Assemblies, ArchitectureAssemblies.Names);
    }
}
