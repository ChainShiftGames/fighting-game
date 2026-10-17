using Sim.Modules;
using Sim.Rules;

namespace Sim.Data.Tests.Modules;

/// <summary>A module with config that only these tests register, so they don't depend on any real module.</summary>
internal sealed class LineupTestModule(int id, string name) : IMechanicModule
{
    public const int MaxStrength = 100;

    public ModuleId Id { get; } = new(id);

    public string Name { get; } = name;

    public ModuleConfigLayout ConfigLayout { get; } = ModuleConfigLayout.For<LineupTestConfig>(
        new ModuleConfigField("strength", 0, MaxStrength),
        new ModuleConfigField("window", 1, 10));

    /// <summary>The module's data file text.</summary>
    public static string File(bool enabled, int strength = 5, int window = 3) =>
        $$"""{ "schemaVersion": 1, "enabled": {{(enabled ? "true" : "false")}}, "strength": {{strength}}, "window": {{window}} }""";
}
