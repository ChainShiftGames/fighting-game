using Sim.Input;

namespace Sim.Tests.Input;

/// <summary>Button sets built in code for tests, so they don't depend on loading data.</summary>
internal static class TestButtonSets
{
    /// <summary>Same layout as <c>data/input/buttons.json</c>.</summary>
    public static ButtonSet SixButtons() => new(
    [
        new ButtonDefinition("A", 4),
        new ButtonDefinition("B", 5),
        new ButtonDefinition("C", 6),
        new ButtonDefinition("D", 7),
        new ButtonDefinition("Special", 8),
        new ButtonDefinition("Dash", 9),
    ]);

    /// <summary>A different count, names and bits, guarding against code that assumes the default set (ADR-0010).</summary>
    public static ButtonSet FiveButtons() => new(
    [
        new ButtonDefinition("Light", 4),
        new ButtonDefinition("Medium", 5),
        new ButtonDefinition("Heavy", 6),
        new ButtonDefinition("Special", 7),
        new ButtonDefinition("Dash", 12),
    ]);
}
