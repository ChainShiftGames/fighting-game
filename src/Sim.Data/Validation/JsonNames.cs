using System;

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
    }

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
        string[] names = new string[_entries.Length];
        for (int i = 0; i < _entries.Length; i++)
        {
            names[i] = $"\"{_entries[i].Name}\"";
        }

        return string.Join(", ", names);
    }
}
