using System;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>An on/off setting.</summary>
public sealed class ToggleSetting : SettingEntry<bool>
{
    public ToggleSetting(
        string labelKey,
        string descriptionKey,
        Func<PlayerSettings, bool> get,
        Func<PlayerSettings, bool, PlayerSettings> set)
        : base(labelKey, descriptionKey, get, set)
    {
    }

    public override MenuItem Item(PlayerSettings settings, Action<PlayerSettings> change) =>
        new ToggleItem(Label, Get(settings), on => change(Set(settings, on)), Enabled(settings))
        {
            Description = DescriptionFor(settings),
        };
}
