using Sim.Rules;

namespace Scenarios.Tests.Runner;

/// <summary>A mechanics module a scenario switches on (<c>setup: module name 5 0</c>), with its raw config values.</summary>
/// <remarks>Modules take lineup slots in the order the setup lists them.</remarks>
public sealed record EnabledModule(string Name, ModuleId Id, int[] Config);
