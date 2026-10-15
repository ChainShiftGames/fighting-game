using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Architecture.Tests;

/// <summary>ADR-0009: naming rules checked on the compiled code (ARCHITECTURE.md §5).</summary>
public sealed class NamingTests
{
    // The .editorconfig naming rule catches this while compiling our own code; this also covers generated code
    // and any project that opts out of code-style enforcement.
    [Fact]
    public void InterfaceNamesStartWithI()
    {
        Interfaces()
            .Should().HaveNameMatching("^I[A-Z]")
            .WithoutRequiringPositiveResults()
            .Check(LoadedArchitecture.Model);
    }
}
