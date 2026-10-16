using System;
using System.Linq;
using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Architecture.Tests;

/// <summary>ADR-0009: only game/ uses Godot; every other runtime assembly stays engine-free (ARCHITECTURE.md §2).</summary>
public sealed class EngineIsolationTests
{
    [Theory]
    [InlineData("Sim")]
    [InlineData("Sim.Data")]
    [InlineData("Netcode")]
    [InlineData("Platform.Steam")]
    public void HasNoCompiledGodotReference(string assemblyName)
    {
        // Contains, not StartsWith: also catches third-party Godot bindings such as "Foo.Godot".
        Assert.DoesNotContain(
            CompiledReferences.Of(assemblyName),
            reference => reference.Contains("Godot", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Sim")]
    [InlineData("Sim.Data")]
    [InlineData("Netcode")]
    [InlineData("Platform.Steam")]
    public void HasNoTypeUsingGodot(string assemblyName)
    {
        Types().That().ResideInAssemblyMatching(TypeSelection.AssemblyPattern(assemblyName))
            // NotDependOnAnyTypesThat, not NotDependOnAny(Types()...): Godot types live outside the loaded
            // assemblies, and Types() only selects types inside them.
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"^Godot(\..+)?$")
            .WithoutRequiringPositiveResults()
            .Check(LoadedArchitecture.Model);
    }
}
