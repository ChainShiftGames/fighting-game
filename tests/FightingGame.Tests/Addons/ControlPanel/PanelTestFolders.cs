using System;
using System.IO;
using FightingGame.Addons.ControlPanel.Documents;
using FightingGame.Presentation.Feel;

namespace FightingGame.Tests.Addons.ControlPanel;

/// <summary>A throwaway copy of data/ and the effect settings file, so control panel tests can save.</summary>
internal sealed class PanelTestFolders : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "control-panel-tests", Guid.NewGuid().ToString("N"));

    public PanelTestFolders()
    {
        Folders = new PanelFolders(Path.Combine(_root, "data"), Path.Combine(_root, "game"));
        Copy(TestGameData.DataRoot, Folders.DataRoot);
        string effects = Folders.FullPath(EffectSettingsCheck.File);
        Directory.CreateDirectory(Path.GetDirectoryName(effects)!);
        string game = Path.Combine(TestGameData.RepositoryRoot, "game");
        File.Copy(Path.Combine(game, EffectSettingsFile.ProjectPath.Replace('/', Path.DirectorySeparatorChar)), effects);
    }

    public PanelFolders Folders { get; }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static void Copy(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (string file in Directory.GetFiles(from))
        {
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        }

        foreach (string folder in Directory.GetDirectories(from))
        {
            Copy(folder, Path.Combine(to, Path.GetFileName(folder)));
        }
    }
}
