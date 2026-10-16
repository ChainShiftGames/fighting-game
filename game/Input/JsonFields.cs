using System.Collections.Generic;
using System.Text.Json;

namespace FightingGame.Input;

/// <summary>
/// Reads the fields of one JSON object in the bindings file, reporting missing, mistyped and unknown fields with
/// their place in the file.
/// </summary>
public sealed class JsonFields
{
    private readonly JsonElement _element;
    private readonly string _path;
    private readonly List<string> _errors;
    private readonly HashSet<string> _read = [];

    /// <param name="path">Where <paramref name="element"/> is in the file, e.g. <c>players[0].gamepad</c>.</param>
    public JsonFields(JsonElement element, string path, List<string> errors)
    {
        _path = path;
        _errors = errors;
        if (element.ValueKind == JsonValueKind.Object)
        {
            _element = element;
        }
        else
        {
            errors.Add($"{path}: must be an object");
        }
    }

    public int? Int(string name) =>
        Required(name, JsonValueKind.Number, "a whole number") is { } value && value.TryGetInt32(out int number)
            ? number
            : null;

    public bool? Bool(string name)
    {
        if (!TryGet(name, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        Error(name, "must be true or false");
        return null;
    }

    /// <summary>A number from 0 to 1.</summary>
    public float? Fraction(string name)
    {
        if (Required(name, JsonValueKind.Number, "a number from 0 to 1") is not { } value)
        {
            return null;
        }

        float number = value.GetSingle();
        if (number is >= 0 and <= 1)
        {
            return number;
        }

        Error(name, "must be a number from 0 to 1");
        return null;
    }

    /// <summary>The nested object; an empty one (with an error) when it is missing.</summary>
    public JsonFields Object(string name)
    {
        string path = $"{_path}.{name}";
        if (TryGet(name, out JsonElement value))
        {
            return new JsonFields(value, path, _errors);
        }

        return new JsonFields(default, path, []);
    }

    public IEnumerable<(JsonElement Element, string Path)> Array(string name)
    {
        if (Required(name, JsonValueKind.Array, "a list") is not { } value)
        {
            yield break;
        }

        int index = 0;
        foreach (JsonElement element in value.EnumerateArray())
        {
            yield return (element, $"{_path}.{name}[{index++}]");
        }
    }

    /// <summary>The fields not read yet, in file order; they count as read afterwards.</summary>
    public IEnumerable<(string Name, JsonElement Value, string Path)> Remaining()
    {
        if (_element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (JsonProperty property in _element.EnumerateObject())
        {
            if (_read.Add(property.Name))
            {
                yield return (property.Name, property.Value, $"{_path}.{property.Name}");
            }
        }
    }

    /// <summary>A list of strings, each with its place in the file.</summary>
    public static IEnumerable<(string Value, string Path)> Strings(JsonElement list, string path, List<string> errors)
    {
        if (list.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{path}: must be a list of names, e.g. [\"W\"]");
            yield break;
        }

        int index = 0;
        foreach (JsonElement element in list.EnumerateArray())
        {
            string elementPath = $"{path}[{index++}]";
            if (element.ValueKind == JsonValueKind.String)
            {
                yield return (element.GetString()!, elementPath);
            }
            else
            {
                errors.Add($"{elementPath}: must be a name in quotes");
            }
        }
    }

    public void Error(string name, string message) => _errors.Add($"{_path}.{name}: {message}");

    /// <summary>Reports every field nothing read: a misspelt field must not be silently ignored.</summary>
    public void Finish()
    {
        foreach ((string name, _, _) in Remaining())
        {
            Error(name, "is not a known field");
        }
    }

    private JsonElement? Required(string name, JsonValueKind kind, string description)
    {
        if (!TryGet(name, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == kind)
        {
            return value;
        }

        Error(name, $"must be {description}");
        return null;
    }

    private bool TryGet(string name, out JsonElement value)
    {
        _read.Add(name);
        value = default;
        if (_element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (_element.TryGetProperty(name, out value))
        {
            return true;
        }

        Error(name, "is missing");
        return false;
    }
}
