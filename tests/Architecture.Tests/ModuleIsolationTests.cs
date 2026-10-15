using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchUnitNET.Fluent.Slices.SliceRuleDefinition;

namespace Architecture.Tests;

/// <summary>ADR-0009 / ADR-0011: mechanics are modules in <c>Sim.Modules.&lt;Name&gt;</c> (ARCHITECTURE.md §5).</summary>
public sealed class ModuleIsolationTests
{
    // A module is everything in Sim.Modules.<Name> and below. Sim.Modules itself holds the module contract
    // (CONVENTIONS.md), which the core uses; it is exempt from the core rule so the one registration line can live
    // there until its home is decided (P1-20).
    private const string ModuleNamespace = @"^Sim\.Modules\.[^.]+(\..+)?$";
    private const string ModulesRootOrModuleNamespace = @"^Sim\.Modules(\..+)?$";

    [Fact]
    public void CoreDoesNotReferenceModules()
    {
        Types().That().ResideInAssemblyMatching(TypeSelection.AssemblyPattern("Sim"))
            .And().DoNotResideInNamespaceMatching(ModulesRootOrModuleNamespace)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ModuleNamespace))
            .WithoutRequiringPositiveResults()
            .Check(LoadedArchitecture.Model);
    }

    [Fact]
    public void ModulesDoNotReferenceEachOther()
    {
        Slices().Matching("Sim.Modules.(*)..")
            .Should().NotDependOnEachOther()
            .Check(LoadedArchitecture.Model);
    }
}
