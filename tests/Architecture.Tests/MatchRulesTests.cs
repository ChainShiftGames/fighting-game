using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
    private const string RulesNamespace = "Sim.Rules";

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

    // ADR-0011 §5: one blittable struct, so it can be hashed for the online check and never shares state through
    // references.
    [Fact]
    public void MatchRulesIsOneBlittableStruct()
    {
        Type[] matchRules = SimTypes().Where(type => type.Name == MatchRulesTypeName).ToArray();

        Type type = Assert.Single(matchRules);
        Assert.Equal(RulesNamespace, type.Namespace);
        Assert.True(type.IsValueType, $"{type.FullName} must be a struct.");
        Assert.False(ContainsReferences(type), $"{type.FullName} must not contain reference-type fields.");
    }

    // A second settings struct in Sim.Rules would be a setting source the rules hash doesn't cover: everything there
    // must be MatchRules or a part of it.
    [Fact]
    public void RulesNamespaceHoldsOnlyMatchRulesAndItsParts()
    {
        Type matchRules = SimTypes().Single(type => type.Name == MatchRulesTypeName);
        var parts = new HashSet<Type>();
        CollectFieldTypes(matchRules, parts);

        foreach (Type type in SimTypes().Where(type => type.Namespace == RulesNamespace))
        {
            Assert.True(parts.Contains(type), $"{type.FullName} is in {RulesNamespace} but not part of MatchRules.");
        }
    }

    // Catches settings types added elsewhere in Sim by name. Module configs are views over their MatchRules slot
    // (IModuleConfig).
    [Fact]
    public void SettingsTypesAreMatchRulesPartsOrModuleConfigs()
    {
        Type moduleConfig = SimTypes().Single(type => type.FullName == "Sim.Modules.IModuleConfig");
        var settingsName = new Regex("(Rules|Settings|Config|Configuration|Options|Tuning)$");

        foreach (Type type in SimTypes().Where(type => settingsName.IsMatch(type.Name)))
        {
            bool allowed = type.Namespace == RulesNamespace || moduleConfig.IsAssignableFrom(type);
            Assert.True(allowed, $"{type.FullName} looks like a setting: put it in MatchRules or a module config.");
        }
    }

    private static Type[] SimTypes() =>
        Assembly.Load("Sim").GetTypes().Where(type => !type.IsNested && type.Namespace is not null).ToArray();

    private static void CollectFieldTypes(Type type, HashSet<Type> parts)
    {
        if (!parts.Add(type))
        {
            return;
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            CollectFieldTypes(field.FieldType, parts);
        }
    }

    private static bool ContainsReferences(Type type) =>
        (bool)typeof(RuntimeHelpers)
            .GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences))!
            .MakeGenericMethod(type)
            .Invoke(null, null)!;
}
