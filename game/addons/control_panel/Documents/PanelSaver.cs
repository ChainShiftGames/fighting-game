using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>
/// Writes the control panel's changed files, but only when every check passes and no file changed on disk since the
/// panel read it (another editor's save is never overwritten silently).
/// </summary>
public sealed class PanelSaver
{
    private static readonly UTF8Encoding _utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly PanelDocument _document;
    private readonly IReadOnlyList<IPanelFileCheck> _checks;

    public PanelSaver(PanelDocument document, IReadOnlyList<IPanelFileCheck> checks)
    {
        _document = document;
        _checks = checks;
    }

    /// <summary>What saving now would report; empty when the changes can be saved.</summary>
    public IReadOnlyList<string> Check()
    {
        var problems = new List<string>();
        foreach (IPanelFileCheck check in _checks)
        {
            problems.AddRange(check.Check(_document.ChangedFiles));
        }

        return problems;
    }

    /// <summary>Writes the changed files when they pass; otherwise writes nothing.</summary>
    /// <returns>The problems that stopped the save; empty when the files were written.</returns>
    public IReadOnlyList<string> Save()
    {
        var problems = new List<string>();
        foreach (PanelFile file in _document.ChangedFiles.Keys)
        {
            string path = _document.Folders.FullPath(file);
            if (!File.Exists(path) || File.ReadAllText(path) != _document.SavedText(file))
            {
                problems.Add($"{file}: changed on disk since the panel read it; Reload to see the new version.");
            }
        }

        problems.AddRange(problems.Count == 0 ? Check() : []);
        if (problems.Count > 0)
        {
            return problems;
        }

        var written = new List<PanelFile>();
        foreach ((PanelFile file, string text) in _document.ChangedFiles)
        {
            File.WriteAllText(_document.Folders.FullPath(file), text, _utf8WithoutBom);
            written.Add(file);
        }

        _document.MarkSaved(written);
        return problems;
    }
}
