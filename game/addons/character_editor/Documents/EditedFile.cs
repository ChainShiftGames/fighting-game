using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// One data file open in the editor: its JSON tree, which editor panels change in place, and whether it changed
/// since it was opened or last saved. Only changed files are written, so saving never touches the others.
/// </summary>
public sealed class EditedFile
{
    /// <param name="path">Data-relative path with '/' separators, e.g. <c>characters/testman/moves/5a.json</c>.</param>
    /// <param name="layout">How the file's lists were wrapped when it was read; none for a new file.</param>
    public EditedFile(string path, JsonObject root, ListLineStarts? layout = null)
    {
        Path = path;
        Root = root;
        Layout = layout ?? ListLineStarts.None;
    }

    public string Path { get; }

    public JsonObject Root { get; }

    /// <summary>Kept when saving, so hand-wrapped lists stay wrapped the same way.</summary>
    public ListLineStarts Layout { get; }

    public bool IsChanged { get; private set; }

    /// <summary>Call after changing <see cref="Root"/>, so the next save writes this file.</summary>
    public void MarkChanged() => IsChanged = true;

    /// <summary>The file on disk now matches <see cref="Root"/>.</summary>
    public void MarkSaved() => IsChanged = false;
}
