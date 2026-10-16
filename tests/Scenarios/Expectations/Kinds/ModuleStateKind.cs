using System.Globalization;
using Sim.State;

namespace Scenarios.Tests.Expectations.Kinds;

/// <summary><c>module &lt;name&gt;[i]</c>: integer i of an enabled module's state slice, e.g. <c>module contract_probe[0]</c>.</summary>
/// <remarks>Raw slice access for any module; a module with a readable meaning can add its own kind instead.</remarks>
public sealed class ModuleStateKind : IExpectationKind
{
    public string Keyword => "module";

    public bool IsPerFighter => false;

    public IExpectation Parse(ExpectationSyntax syntax)
    {
        if (syntax.Arguments.Count == 0)
        {
            throw syntax.Error("write 'module <name>[<index>] <comparison> <value>'");
        }

        (string name, int index) = ParseSlot(syntax.Arguments[0], syntax);
        int slot = syntax.Setup.ModuleSlot(name);
        if (slot < 0)
        {
            throw syntax.Error($"module {name} isn't enabled; add 'setup: module {name}'");
        }

        (Comparison comparison, string valueText) = syntax.ComparisonAt(1, isOrdered: true);
        if (!int.TryParse(valueText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int expected))
        {
            throw syntax.Error($"'{valueText}' isn't an integer");
        }

        return new ValueExpectation(
            syntax.Text,
            frame => frame.After.Modules[slot][index],
            value => value.ToString(CultureInfo.InvariantCulture),
            comparison,
            expected);
    }

    private static (string Name, int Index) ParseSlot(string text, ExpectationSyntax syntax)
    {
        int open = text.IndexOf('[', System.StringComparison.Ordinal);
        int index = -1;
        bool isValid = open > 0 && text.EndsWith(']') &&
            int.TryParse(text[(open + 1)..^1], NumberStyles.None, CultureInfo.InvariantCulture, out index) &&
            index < ModuleStateSlice.Length;
        if (!isValid)
        {
            throw syntax.Error(
                $"'{text}' isn't '<name>[<index>]' with an index from 0 to {ModuleStateSlice.Length - 1}");
        }

        return (text[..open], index);
    }
}
