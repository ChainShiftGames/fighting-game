using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;
using FightingGame.Presentation.Feel;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>The Effects page: every field of the effect settings file (<see cref="EffectSettingFields"/>).</summary>
public static class EffectSettingList
{
    public static IReadOnlyList<SettingGroup> All() =>
    [
        Group("Screen shake", EffectSettingFields.ShakeSection, "Shakes start at full strength and fade to nothing."),
        Group(
            "Hit sparks",
            EffectSettingFields.SparkSection,
            "A spark's weight (0 to 1) comes from the hit's hitstop. Its look is tuned in the VFX Lab."),
        Group("Controller rumble", EffectSettingFields.RumbleSection, "Strength 0 to 1 per pulse."),
    ];

    private static SettingGroup Group(string title, string section, string hint)
    {
        var settings = new List<PanelSetting>();
        foreach (EffectSettingField field in EffectSettingFields.All)
        {
            if (field.Section != section)
            {
                continue;
            }

            PanelFile file = EffectSettingsCheck.File;
            PanelSetting setting = field.Whole
                ? PanelSetting.Whole(field.Label, file, field.Path, (int)field.Min, (int)field.Max, (int)field.Step)
                : PanelSetting.Decimal(field.Label, file, field.Path, field.Min, field.Max, field.Step);
            settings.Add(setting with { Hint = field.Hint });
        }

        return new SettingGroup(title, settings) { Hint = hint };
    }
}
