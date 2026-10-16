using System;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.xUnitV3;
using Netcode.Drivers;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Architecture.Tests;

/// <summary>
/// P2-01 / ADR-0005 §2: the game only talks to <see cref="IMatchDriver{TState}"/>. Its composition roots (the match
/// startups) are the only types that name a concrete driver, so switching drivers changes nothing else.
/// </summary>
public sealed class MatchDriverChoiceTests
{
    private const string CompositionRootSuffix = "Startup";

    [Fact]
    public void OnlyTheGamesMatchStartupsNameAConcreteDriver()
    {
        Types().That().ResideInAssemblyMatching(TypeSelection.AssemblyPattern("FightingGame"))
            .And().DoNotHaveNameEndingWith(CompositionRootSuffix)
            .Should().NotDependOnAnyTypesThat().HaveFullNameMatching(ConcreteDriverPattern())
            .WithoutRequiringPositiveResults()
            .Check(LoadedArchitecture.Model);
    }

    [Fact]
    public void TheRuleKnowsEveryDriver()
    {
        // Guards the pattern above: an empty driver list would make the rule pass without checking anything.
        Assert.Contains("LocalMatchDriver`1", ConcreteDriverPattern(), StringComparison.Ordinal);
        Assert.Contains("RollbackMatchDriver`1", ConcreteDriverPattern(), StringComparison.Ordinal);
    }

    // Every class in Netcode that implements the driver interface, matched by its generic definition's full name.
    private static string ConcreteDriverPattern()
    {
        string[] drivers = typeof(IMatchDriver<>).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.GetInterfaces().Any(IsMatchDriver))
            .Select(type => Regex.Escape(type.FullName!))
            .Order(StringComparer.Ordinal)
            .ToArray();
        return $"^({string.Join('|', drivers)})$";
    }

    private static bool IsMatchDriver(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IMatchDriver<>);
}
