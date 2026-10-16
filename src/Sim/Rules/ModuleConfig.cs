using System.Runtime.CompilerServices;

namespace Sim.Rules;

/// <summary>One module's settings as raw integers; the module reads them through its own config struct.</summary>
[InlineArray(Length)]
public struct ModuleConfig
{
    /// <summary>Integers per module.</summary>
    public const int Length = 16;

    private int _element;
}
