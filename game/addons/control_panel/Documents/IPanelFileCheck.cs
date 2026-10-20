using System.Collections.Generic;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>Checks the control panel's changed files the way the game will read them, before anything is saved.</summary>
public interface IPanelFileCheck
{
    /// <param name="changed">Every changed file's new text.</param>
    /// <returns>One line per problem; empty when the files are fine to save.</returns>
    IReadOnlyList<string> Check(IReadOnlyDictionary<PanelFile, string> changed);
}
