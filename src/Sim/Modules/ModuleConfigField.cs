namespace Sim.Modules;

/// <summary>One integer of a module's config struct: its key in the module's data file and the range data may set.</summary>
public readonly struct ModuleConfigField
{
    /// <param name="key">The camelCase key in <c>data/rules/modules/&lt;name&gt;.json</c>.</param>
    /// <param name="min">The smallest value data may set.</param>
    /// <param name="max">The largest value data may set.</param>
    public ModuleConfigField(string key, int min, int max)
    {
        Key = key;
        Min = min;
        Max = max;
    }

    public string Key { get; }

    public int Min { get; }

    public int Max { get; }
}
