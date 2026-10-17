using System.Globalization;
using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>p1 mechanic 0</c>: integer 0 of the state slice of P1's character mechanic (ADR-0023).</summary>
/// <remarks>The character's data switches its mechanic on, so no setup line is needed; without one the slice stays 0.</remarks>
public sealed class MechanicStateKind : IExpectationKind
{
    public string Keyword => "mechanic";

    public bool IsPerFighter => true;

    public IExpectation Parse(ExpectationSyntax syntax)
    {
        if (syntax.Arguments.Count == 0 ||
            !int.TryParse(syntax.Arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
            index >= ModuleStateSlice.Length)
        {
            throw syntax.Error(
                $"write 'p<n> mechanic <index> <comparison> <value>' with an index from 0 to {ModuleStateSlice.Length - 1}");
        }

        (Comparison comparison, string valueText) = syntax.ComparisonAt(1, isOrdered: true);
        if (!int.TryParse(valueText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int expected))
        {
            throw syntax.Error($"'{valueText}' isn't an integer");
        }

        int slot = ModuleStates.MechanicSlot(syntax.Player);
        return new ValueExpectation(
            syntax.Text,
            frame => frame.After.Modules[slot][index],
            value => value.ToString(CultureInfo.InvariantCulture),
            comparison,
            expected);
    }
}
