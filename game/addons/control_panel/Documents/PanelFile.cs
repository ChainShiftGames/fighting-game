namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>One file the control panel edits.</summary>
/// <param name="Path">Relative to its area's folder, with '/' separators, e.g. <c>rules/match.json</c>.</param>
public readonly record struct PanelFile(PanelFileArea Area, string Path)
{
    public static PanelFile InData(string path) => new(PanelFileArea.Data, path);

    public static PanelFile InGame(string path) => new(PanelFileArea.Game, path);

    public override string ToString() => Area == PanelFileArea.Data ? $"data/{Path}" : $"game/{Path}";
}
