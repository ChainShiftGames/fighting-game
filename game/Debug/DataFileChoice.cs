using System;
using System.Collections.Generic;

namespace FightingGame.Debug;

/// <summary>
/// A developer setting stored in a <c>data/</c> file as one of a fixed set of names (a JSON string enum, e.g.
/// <c>tradeRule</c>); the debug panel edits it in place, like <see cref="DataFileSetting"/> does numbers.
/// </summary>
public sealed class DataFileChoice
{
    /// <param name="file">Data-relative path with '/' separators, e.g. <c>rules/match.json</c>.</param>
    /// <param name="key">The top-level field in that file.</param>
    /// <param name="choices">The names Sim.Data accepts, in the order the panel lists them.</param>
    public DataFileChoice(string name, string file, string key, IReadOnlyList<string> choices)
    {
        Name = name;
        File = file;
        Key = key;
        Choices = choices;
    }

    public string Name { get; }

    public string File { get; }

    public string Key { get; }

    public IReadOnlyList<string> Choices { get; }

    /// <summary>The position of <paramref name="value"/> in <see cref="Choices"/>; -1 when it isn't one.</summary>
    public int IndexOf(string value)
    {
        for (int i = 0; i < Choices.Count; i++)
        {
            if (string.Equals(Choices[i], value, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    /// <exception cref="FormatException">The file doesn't hold the field as one of <see cref="Choices"/>.</exception>
    public string Read(string dataRoot)
    {
        string value = JsonStringField.Read(System.IO.File.ReadAllText(DataFilePath.In(dataRoot, File)), Key);
        return IndexOf(value) >= 0
            ? value
            : throw new FormatException($"'{Key}' is \"{value}\", which isn't one of {string.Join(", ", Choices)}.");
    }

    /// <exception cref="ArgumentOutOfRangeException">The value isn't one of <see cref="Choices"/>.</exception>
    public void Write(string dataRoot, string value)
    {
        if (IndexOf(value) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"'{Key}' must be one of {string.Join(", ", Choices)}.");
        }

        string path = DataFilePath.In(dataRoot, File);
        System.IO.File.WriteAllText(path, JsonStringField.Write(System.IO.File.ReadAllText(path), Key, value));
    }
}
