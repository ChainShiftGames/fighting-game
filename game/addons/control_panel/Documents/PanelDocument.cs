using System;
using System.Collections.Generic;
using System.IO;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>
/// The text of every file the control panel shows, as saved and as edited, and the setting values read from it.
/// Edits stay in memory until <see cref="PanelSaver"/> writes them.
/// </summary>
/// <remarks>
/// Values are always read from the text, never cached apart from it, so what the panel shows is what saving writes.
/// An edit that puts a file back to its saved text drops it from the changed files.
/// </remarks>
public sealed class PanelDocument
{
    private readonly Dictionary<PanelFile, string> _saved = [];
    private readonly Dictionary<PanelFile, string> _edited = [];

    public PanelDocument(PanelFolders folders)
    {
        Folders = folders;
    }

    /// <summary>Raised after any edit, revert or reload.</summary>
    public event Action? Changed;

    public PanelFolders Folders { get; }

    public bool HasChanges => _edited.Count > 0;

    /// <summary>The changed files' edited text.</summary>
    public IReadOnlyDictionary<PanelFile, string> ChangedFiles => _edited;

    public bool Exists(PanelFile file) => _edited.ContainsKey(file) || File.Exists(Folders.FullPath(file));

    /// <summary>The file as edited, or as on disk when it isn't changed.</summary>
    /// <exception cref="IOException">The file can't be read.</exception>
    public string Text(PanelFile file) => _edited.TryGetValue(file, out string? text) ? text : SavedText(file);

    /// <summary>The file as it was on disk when first read or last saved.</summary>
    /// <exception cref="IOException">The file can't be read.</exception>
    public string SavedText(PanelFile file)
    {
        if (!_saved.TryGetValue(file, out string? text))
        {
            text = File.ReadAllText(Folders.FullPath(file));
            _saved[file] = text;
        }

        return text;
    }

    /// <summary>True when the setting's file holds its value (optional fields may be left out).</summary>
    public bool Has(PanelSetting setting) =>
        Exists(setting.File) && JsonPathField.Has(Text(setting.File), setting.Path);

    /// <exception cref="FormatException">The file doesn't hold the setting as a number.</exception>
    public double Number(PanelSetting setting) => JsonPathField.ReadNumber(Text(setting.File), setting.Path);

    /// <exception cref="FormatException">The file doesn't hold the setting as true or false.</exception>
    public bool Flag(PanelSetting setting) => JsonPathField.ReadBool(Text(setting.File), setting.Path);

    /// <exception cref="FormatException">The file doesn't hold the setting as a string.</exception>
    public string Choice(PanelSetting setting) => JsonPathField.ReadString(Text(setting.File), setting.Path);

    /// <summary>True when the setting's edited value differs from its saved one.</summary>
    public bool IsChanged(PanelSetting setting)
    {
        if (!_edited.TryGetValue(setting.File, out string? edited))
        {
            return false;
        }

        try
        {
            return ValueText(edited, setting) != ValueText(SavedText(setting.File), setting);
        }
        catch (Exception exception) when (exception is FormatException or IOException)
        {
            return true;
        }
    }

    /// <summary>Sets a number, clamped into range and rounded as the file stores it.</summary>
    public void SetNumber(PanelSetting setting, double value) => SetNumbers([(setting, value)]);

    /// <summary>Sets several numbers as one edit, so listeners refresh once.</summary>
    public void SetNumbers(IEnumerable<(PanelSetting Setting, double Value)> values)
    {
        foreach ((PanelSetting setting, double value) in values)
        {
            string text = Text(setting.File);
            Put(setting.File, JsonPathField.WriteNumber(text, setting.Path, setting.Fit(value), setting.Decimals));
        }

        Changed?.Invoke();
    }

    public void SetFlag(PanelSetting setting, bool value)
    {
        Put(setting.File, JsonPathField.WriteBool(Text(setting.File), setting.Path, value));
        Changed?.Invoke();
    }

    public void SetChoice(PanelSetting setting, string value)
    {
        Put(setting.File, JsonPathField.WriteString(Text(setting.File), setting.Path, value));
        Changed?.Invoke();
    }

    /// <summary>Puts one setting back to its saved value, keeping other edits in its file.</summary>
    public void Revert(PanelSetting setting)
    {
        string saved = SavedText(setting.File);
        string current = Text(setting.File);
        string path = setting.Path;
        string text = setting.Kind switch
        {
            SettingKind.Flag => JsonPathField.WriteBool(current, path, JsonPathField.ReadBool(saved, path)),
            SettingKind.Choice => JsonPathField.WriteString(current, path, JsonPathField.ReadString(saved, path)),
            _ => JsonPathField.WriteNumber(current, path, JsonPathField.ReadNumber(saved, path), setting.Decimals),
        };
        Put(setting.File, text);
        Changed?.Invoke();
    }

    /// <summary>Sets whole files' edited text as one edit, e.g. edits kept across a hot reload.</summary>
    public void RestoreTexts(IEnumerable<(PanelFile File, string Text)> files)
    {
        foreach ((PanelFile file, string text) in files)
        {
            Put(file, text);
        }

        Changed?.Invoke();
    }

    /// <summary>Drops every edit.</summary>
    public void RevertAll()
    {
        _edited.Clear();
        Changed?.Invoke();
    }

    /// <summary>Drops every edit and reads the files from disk again, e.g. after another editor saved them.</summary>
    public void Reload()
    {
        _edited.Clear();
        _saved.Clear();
        Changed?.Invoke();
    }

    /// <summary>Records that these files' edited text is now on disk.</summary>
    public void MarkSaved(IReadOnlyCollection<PanelFile> files)
    {
        foreach (PanelFile file in files)
        {
            _saved[file] = _edited[file];
            _edited.Remove(file);
        }

        Changed?.Invoke();
    }

    private void Put(PanelFile file, string text)
    {
        if (text == SavedText(file))
        {
            _edited.Remove(file);
        }
        else
        {
            _edited[file] = text;
        }
    }

    private static string ValueText(string json, PanelSetting setting) => setting.Kind switch
    {
        SettingKind.Flag => JsonPathField.ReadBool(json, setting.Path) ? "true" : "false",
        SettingKind.Choice => JsonPathField.ReadString(json, setting.Path),
        _ => JsonPathField.NumberText(JsonPathField.ReadNumber(json, setting.Path), setting.Decimals),
    };
}
