using System.Collections.Generic;
using FightingGame.Presentation.Feel;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>Reads a changed effect settings file with the game's own reader (<see cref="EffectSettingsFile"/>).</summary>
public sealed class EffectSettingsCheck : IPanelFileCheck
{
    public static PanelFile File { get; } = PanelFile.InGame(EffectSettingsFile.ProjectPath);

    public IReadOnlyList<string> Check(IReadOnlyDictionary<PanelFile, string> changed)
    {
        if (!changed.TryGetValue(File, out string? text) || EffectSettingsFile.Read(text, out string error) is not null)
        {
            return [];
        }

        return [$"{File}: {error}"];
    }
}
