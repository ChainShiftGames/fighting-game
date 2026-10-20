namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>Which folder a file the control panel edits lives in, which decides how it is checked before saving.</summary>
public enum PanelFileArea
{
    /// <summary><c>data/</c>: game data, checked by loading all of it with Sim.Data.</summary>
    Data,

    /// <summary><c>game/</c>: presentation settings files, each checked by its own reader.</summary>
    Game,
}
