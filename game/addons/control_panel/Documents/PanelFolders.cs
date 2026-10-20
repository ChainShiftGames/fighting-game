using FightingGame.Debug;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>The full paths of the folders the control panel's files live in.</summary>
/// <param name="DataRoot">The repository's <c>data/</c> folder.</param>
/// <param name="GameRoot">The Godot project folder, <c>game/</c>.</param>
public sealed record PanelFolders(string DataRoot, string GameRoot)
{
    public string FullPath(PanelFile file) =>
        DataFilePath.In(file.Area == PanelFileArea.Data ? DataRoot : GameRoot, file.Path);
}
