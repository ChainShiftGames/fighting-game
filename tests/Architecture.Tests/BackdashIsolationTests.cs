using System;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// ADR-0005 §2 / ARCHITECTURE.md §8: Backdash is only used behind the match-driver interface, inside Netcode.
/// Transports (Steam) implement Netcode's <c>ITransport</c> instead, so Backdash can be replaced or forked in one place.
/// </summary>
public sealed class BackdashIsolationTests
{
    [Theory]
    [InlineData("Sim")]
    [InlineData("Sim.Data")]
    [InlineData("Platform.Steam")]
    [InlineData("FightingGame")]
    public void HasNoCompiledBackdashReference(string assemblyName)
    {
        Assert.DoesNotContain(
            CompiledReferences.Of(assemblyName),
            reference => reference.StartsWith("Backdash", StringComparison.OrdinalIgnoreCase));
    }
}
