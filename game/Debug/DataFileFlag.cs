namespace FightingGame.Debug;

/// <summary>
/// An on/off developer setting stored in a <c>data/</c> file (e.g. a module's <c>enabled</c>); the debug panel edits
/// it in place, like <see cref="DataFileSetting"/> does numbers.
/// </summary>
public sealed class DataFileFlag
{
    /// <param name="file">Data-relative path with '/' separators, e.g. <c>rules/modules/meter.json</c>.</param>
    /// <param name="key">The top-level field in that file.</param>
    public DataFileFlag(string name, string file, string key)
    {
        Name = name;
        File = file;
        Key = key;
    }

    public string Name { get; }

    public string File { get; }

    public string Key { get; }

    /// <exception cref="System.FormatException">The file doesn't hold the field as true or false.</exception>
    public bool Read(string dataRoot) =>
        JsonBoolField.Read(System.IO.File.ReadAllText(DataFilePath.In(dataRoot, File)), Key);

    public void Write(string dataRoot, bool value)
    {
        string path = DataFilePath.In(dataRoot, File);
        System.IO.File.WriteAllText(path, JsonBoolField.Write(System.IO.File.ReadAllText(path), Key, value));
    }
}
