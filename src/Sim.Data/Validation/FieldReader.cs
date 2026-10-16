using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Sim.Data.Validation;

/// <summary>
/// Reads the fields of one JSON object and reports every wrong, missing or unknown field with its file and path.
/// </summary>
/// <remarks>
/// Readers never throw on bad data: they report to <see cref="DataErrors"/> and return a neutral value so loading
/// can continue and list all problems at once. Call <see cref="Finish"/> after reading every field.
/// Numbers must be whole numbers in range; fractions, exponents and out-of-range values are errors, never rounded.
/// </remarks>
public sealed class FieldReader
{
    private const int MaxIdLength = 32;

    private readonly JsonElement _object;
    private readonly string _path;
    private readonly DataErrors _errors;
    private readonly List<string> _knownFields = [];

    internal FieldReader(JsonElement jsonObject, string file, string path, DataErrors errors)
    {
        _object = jsonObject;
        File = file;
        _path = path;
        _errors = errors;
    }

    /// <summary>The file this object came from, relative to the data root.</summary>
    public string File { get; }

    /// <summary>Full JSON path of a field of this object, as used in error messages.</summary>
    public string PathOf(string field) => _path.Length == 0 ? field : $"{_path}.{field}";

    /// <summary>Reports a rule broken by a field that was read successfully (e.g. a frame outside the move).</summary>
    public void Error(string field, string reason) => _errors.Add(File, PathOf(field), reason);

    public int RequiredInt(string name, int min, int max) =>
        TryGet(name, required: true, out JsonElement value) ? ToInt(value, PathOf(name), min, max) : min;

    /// <summary>The value, or <paramref name="absent"/> when the field is left out.</summary>
    public int OptionalInt(string name, int absent, int min, int max) =>
        TryGet(name, required: false, out JsonElement value) ? ToInt(value, PathOf(name), min, max) : absent;

    public bool OptionalBool(string name, bool absent)
    {
        if (!TryGet(name, required: false, out JsonElement value))
        {
            return absent;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        Report(PathOf(name), "must be true or false", value);
        return absent;
    }

    /// <summary>A non-empty string, e.g. a display name.</summary>
    public string RequiredText(string name)
    {
        if (!TryGet(name, required: true, out JsonElement value))
        {
            return "";
        }

        if (value.ValueKind == JsonValueKind.String && value.GetString()!.Trim().Length > 0)
        {
            return value.GetString()!;
        }

        Report(PathOf(name), "must be a non-empty string", value);
        return "";
    }

    /// <summary>A lowercase snake_case id (CONVENTIONS.md), or null when it is wrong.</summary>
    public string? RequiredId(string name) =>
        TryGet(name, required: true, out JsonElement value) ? ToId(value, PathOf(name)) : null;

    public string? OptionalId(string name) =>
        TryGet(name, required: false, out JsonElement value) ? ToId(value, PathOf(name)) : null;

    public TEnum RequiredChoice<TEnum>(string name, JsonNames<TEnum> names)
        where TEnum : struct, Enum =>
        TryGet(name, required: true, out JsonElement value) ? ToChoice(value, PathOf(name), names) : default;

    public TEnum OptionalChoice<TEnum>(string name, JsonNames<TEnum> names, TEnum absent)
        where TEnum : struct, Enum =>
        TryGet(name, required: false, out JsonElement value) ? ToChoice(value, PathOf(name), names) : absent;

    public FieldReader? RequiredObject(string name) =>
        TryGet(name, required: true, out JsonElement value) ? ToObject(value, PathOf(name)) : null;

    public FieldReader? OptionalObject(string name) =>
        TryGet(name, required: false, out JsonElement value) ? ToObject(value, PathOf(name)) : null;

    /// <summary>The array's objects in order; an element that isn't an object is reported and returned as null.</summary>
    public IReadOnlyList<FieldReader?> ObjectArray(string name, bool required) =>
        ReadArray(name, required, ToObject);

    /// <summary>The array's ids in order; an element that isn't a valid id is reported and returned as null.</summary>
    public IReadOnlyList<string?> IdArray(string name, bool required) => ReadArray(name, required, ToId);

    public IReadOnlyList<TEnum> ChoiceArray<TEnum>(string name, JsonNames<TEnum> names, bool required)
        where TEnum : struct, Enum => ReadArray(name, required, (value, path) => ToChoice(value, path, names));

    /// <summary>True when the object has the field, read or not; for choosing between alternative fields.</summary>
    public bool Has(string name) => _object.TryGetProperty(name, out _);

    /// <summary>Reports <paramref name="name"/> if present: a field that this kind of object must not have.</summary>
    public void Forbid(string name, string reason)
    {
        _knownFields.Add(name);
        if (_object.TryGetProperty(name, out _))
        {
            _errors.Add(File, PathOf(name), reason);
        }
    }

    /// <summary>Marks a field as allowed without reading it (e.g. <c>$schema</c>, which only editors use).</summary>
    public void Ignore(string name) => _knownFields.Add(name);

    /// <summary>Reports fields this reader never asked for (typos, removed fields) and fields that appear twice.</summary>
    public void Finish()
    {
        List<string> seen = [];
        foreach (JsonProperty property in _object.EnumerateObject())
        {
            if (seen.Contains(property.Name))
            {
                _errors.Add(File, PathOf(property.Name), "field appears more than once");
                continue;
            }

            seen.Add(property.Name);
            if (!_knownFields.Contains(property.Name))
            {
                _errors.Add(File, PathOf(property.Name),
                    $"unknown field (expected one of: {string.Join(", ", _knownFields)})");
            }
        }
    }

    private bool TryGet(string name, bool required, out JsonElement value)
    {
        _knownFields.Add(name);
        if (_object.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null)
        {
            return true;
        }

        if (required)
        {
            _errors.Add(File, PathOf(name), "required field is missing");
        }

        return false;
    }

    private IReadOnlyList<T> ReadArray<T>(string name, bool required, Func<JsonElement, string, T> readElement)
    {
        if (!TryGet(name, required, out JsonElement value))
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            Report(PathOf(name), "must be an array", value);
            return [];
        }

        List<T> elements = [];
        int index = 0;
        foreach (JsonElement element in value.EnumerateArray())
        {
            elements.Add(readElement(element, $"{PathOf(name)}[{index}]"));
            index++;
        }

        return elements;
    }

    private int ToInt(JsonElement value, string path, int min, int max)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number)
            && number >= min && number <= max && IsPlainInteger(value))
        {
            return number;
        }

        Report(path, $"must be a whole number from {min} to {max}", value);
        return min;
    }

    // TryGetInt32 accepts "5.0" and "5e0"; the data format only allows plain digits so values read the same in
    // every tool.
    private static bool IsPlainInteger(JsonElement value)
    {
        string raw = value.GetRawText();
        return raw.IndexOfAny(['.', 'e', 'E']) < 0;
    }

    private string? ToId(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.String && IsId(value.GetString()!))
        {
            return value.GetString();
        }

        Report(path, $"must be a lowercase snake_case id (a-z, 0-9, single '_', at most {MaxIdLength} characters)",
            value);
        return null;
    }

    private static bool IsId(string text)
    {
        if (text.Length == 0 || text.Length > MaxIdLength || text[0] == '_' || text[^1] == '_')
        {
            return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool valid = c is >= 'a' and <= 'z' or >= '0' and <= '9' || (c == '_' && text[i - 1] != '_');
            if (!valid)
            {
                return false;
            }
        }

        return true;
    }

    private TEnum ToChoice<TEnum>(JsonElement value, string path, JsonNames<TEnum> names)
        where TEnum : struct, Enum
    {
        if (value.ValueKind == JsonValueKind.String && names.TryGetValue(value.GetString()!, out TEnum choice))
        {
            return choice;
        }

        Report(path, $"must be one of {names.Describe()}", value);
        return default;
    }

    private FieldReader? ToObject(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return new FieldReader(value, File, path, _errors);
        }

        Report(path, "must be an object", value);
        return null;
    }

    private void Report(string path, string reason, JsonElement actual)
    {
        string raw = actual.GetRawText();
        string shown = raw.Length > 40 ? raw[..40] + "…" : raw;
        _errors.Add(File, path, $"{reason}, got {shown}");
    }
}
