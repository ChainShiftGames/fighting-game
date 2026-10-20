using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>A titled block of settings on one of the control panel's pages.</summary>
/// <param name="Switch">An on/off setting that turns the group's other settings on; null when they always apply.</param>
public sealed record SettingGroup(string Title, IReadOnlyList<PanelSetting> Settings, PanelSetting? Switch = null)
{
    /// <summary>One line under the title.</summary>
    public string Hint { get; init; } = string.Empty;
}
