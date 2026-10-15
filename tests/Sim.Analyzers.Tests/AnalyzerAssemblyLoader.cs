using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Sim.Analyzers.Tests;

/// <summary>Loads analyzer DLLs into the test process.</summary>
internal sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    // Nothing to register: each analyzer's dependencies sit next to it, where LoadFrom looks.
    public void AddDependencyLocation(string fullPath) { }

    public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
}
