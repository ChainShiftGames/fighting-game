using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// One data file open in the editor: its JSON tree, which editor panels change in place, and whether it changed
/// since it was opened or last saved. Only changed files are written, so saving never touches the others.
/// </summary>
public sealed class EditedFile
{
    /// <param name="path">Data-relative path with '/' separators, e.g. <c>characters/testman/moves/5a.json</c>.</param>
    public EditedFile(string path, JsonObject root)
    {
        Path = path;
        Root = root;
    }

    public string Path { get; }

    public JsonObject Root { get; }

    public bool IsChanged { get; private set; }

    /// <summary>Call after changing <see cref="Root"/>, so the next save writes this file.</summary>
    public void MarkChanged() => IsChanged = true;

    /// <summary>The file on disk now matches <see cref="Root"/>.</summary>
    public void MarkSaved() => IsChanged = false;
}
