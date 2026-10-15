using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Architecture.Tests;

/// <summary>ADR-0009 / ADR-0011: Sim reads gameplay settings only from <c>MatchRules</c>.</summary>
public sealed class MatchRulesTests
{
    // Every way the logic could read a setting from somewhere other than the MatchRules value it is handed:
    // files, environment variables, runtime switches, configuration libraries and serializers.
    private const string ConfigurationNamespaces =
        @"^(System\.IO|System\.Configuration|System\.Text\.Json|System\.Xml|" +
        @"Microsoft\.Extensions\.Configuration|Microsoft\.Extensions\.Options)(\..+)?$";

    private const string MatchRulesTypeName = "MatchRules";

    [Fact]
    public void SimReadsNoOtherSettingSource()
    {
        Types().That().ResideInAssemblyMatching(TypeSelection.AssemblyPattern("Sim"))
            // NotDependOnAnyTypesThat: these are framework types, outside the loaded assemblies.
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(ConfigurationNamespaces)
            .AndShould().NotDependOnAnyTypesThat().HaveFullName("System.Environment")
            .AndShould().NotDependOnAnyTypesThat().HaveFullName("System.AppContext")
            .WithoutRequiringPositiveResults()
            .Check(LoadedArchitecture.Model);
    }

    // Passes while MatchRules doesn't exist yet (P1-20). From then on it holds MatchRules to ADR-0011 §5: one
    // blittable struct, so it can be hashed for the online check and never shares state through references.
    [Fact]
    public void MatchRulesIsOneBlittableStruct()
    {
        Type[] matchRules = Assembly.Load("Sim").GetTypes()
            .Where(type => type.Name == MatchRulesTypeName)
            .ToArray();

        Assert.True(matchRules.Length <= 1, $"Sim defines {matchRules.Length} types named {MatchRulesTypeName}.");
        foreach (Type type in matchRules)
        {
            Assert.True(type.IsValueType, $"{type.FullName} must be a struct.");
            Assert.False(ContainsReferences(type), $"{type.FullName} must not contain reference-type fields.");
        }
    }

    private static bool ContainsReferences(Type type) =>
        (bool)typeof(RuntimeHelpers)
            .GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences))!
            .MakeGenericMethod(type)
            .Invoke(null, null)!;
}
