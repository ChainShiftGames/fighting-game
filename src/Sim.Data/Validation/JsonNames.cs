using System;
using System.Collections.Generic;

namespace Sim.Data.Validation;

/// <summary>The JSON spellings of an enum's values, e.g. <c>"overhead"</c> → <c>Guard.Overhead</c>.</summary>
/// <remarks>Explicit pairs instead of enum reflection, so renaming a C# member never changes the data format.</remarks>
public sealed class JsonNames<TEnum>
    where TEnum : struct, Enum
{
    private readonly (string Name, TEnum Value)[] _entries;

    public JsonNames(params (string Name, TEnum Value)[] entries)
    {
        _entries = entries;
        string[] names = new string[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            names[i] = entries[i].Name;
        }

        Names = names;
    }

    /// <summary>All accepted names in declaration order, e.g. for an editor's choice list.</summary>
    public IReadOnlyList<string> Names { get; }

    public bool TryGetValue(string name, out TEnum value)
    {
        foreach ((string entryName, TEnum entryValue) in _entries)
        {
            if (string.Equals(entryName, name, StringComparison.Ordinal))
            {
                value = entryValue;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>All accepted names, for error messages: <c>"mid", "low", "overhead"</c>.</summary>
    public string Describe()
    {
        string[] quoted = new string[_entries.Length];
        for (int i = 0; i < _entries.Length; i++)
        {
            quoted[i] = $"\"{_entries[i].Name}\"";
        }

        return string.Join(", ", quoted);
    }
}
