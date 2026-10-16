using Sim.Input;

namespace Sim.Tests.Input;

/// <summary>Button sets built in code for tests, so they don't depend on loading data.</summary>
internal static class TestButtonSets
{
    /// <summary>Same layout as <c>data/input/buttons.json</c>.</summary>
    public static ButtonSet SixButtons() => new(
    [
        new ButtonDefinition("a", 4),
        new ButtonDefinition("b", 5),
        new ButtonDefinition("c", 6),
        new ButtonDefinition("d", 7),
        new ButtonDefinition("special", 8),
        new ButtonDefinition("dash", 9),
    ]);

    /// <summary>A different count, names and bits, guarding against code that assumes the default set (ADR-0010).</summary>
    public static ButtonSet FiveButtons() => new(
    [
        new ButtonDefinition("light", 4),
        new ButtonDefinition("medium", 5),
        new ButtonDefinition("heavy", 6),
        new ButtonDefinition("special", 7),
        new ButtonDefinition("dash", 12),
    ]);
}
