namespace Sim.Modules;

/// <summary>One integer of a module's config struct: its key in the module's data file and the range data may set.</summary>
public readonly struct ModuleConfigField
{
    /// <param name="key">The camelCase key in <c>data/rules/modules/&lt;name&gt;.json</c>.</param>
    /// <param name="min">The smallest value data may set.</param>
    /// <param name="max">The largest value data may set.</param>
    /// <param name="hint">One line on what the value does, for the developer control panel; read at load only.</param>
    public ModuleConfigField(string key, int min, int max, string hint = "")
    {
        Key = key;
        Min = min;
        Max = max;
        Hint = hint;
    }

    public string Key { get; }

    public int Min { get; }

    public int Max { get; }

    public string Hint { get; }
}
