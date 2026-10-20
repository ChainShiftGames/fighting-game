using System;
using System.Collections.Generic;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>
/// The control panel's unsaved edits as plain text by file key, so they can outlive the C# assembly: a hot reload
/// rebuilds the panel from scratch, and the edits are put back into the new document.
/// </summary>
public static class PanelEditStash
{
    private const char Separator = '|';

    /// <summary>Every changed file's edited text, by <c>Area|path</c>.</summary>
    public static Dictionary<string, string> Take(PanelDocument document)
    {
        var edits = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((PanelFile file, string text) in document.ChangedFiles)
        {
            edits[KeyOf(file)] = text;
        }

        return edits;
    }

    /// <summary>Lays the stashed edits over the files as they are on disk now.</summary>
    /// <returns>How many files got their edits back; unknown keys are skipped.</returns>
    public static int Restore(PanelDocument document, IReadOnlyDictionary<string, string> edits)
    {
        var files = new List<(PanelFile, string)>();
        foreach ((string key, string text) in edits)
        {
            if (FileOf(key) is PanelFile file && document.Exists(file))
            {
                files.Add((file, text));
            }
        }

        document.RestoreTexts(files);
        return files.Count;
    }

    private static string KeyOf(PanelFile file) => $"{file.Area}{Separator}{file.Path}";

    private static PanelFile? FileOf(string key)
    {
        int split = key.IndexOf(Separator, StringComparison.Ordinal);
        return split > 0 && Enum.TryParse(key[..split], out PanelFileArea area)
            ? new PanelFile(area, key[(split + 1)..])
            : null;
    }
}
