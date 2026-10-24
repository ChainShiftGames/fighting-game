using System;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>A number setting moved in steps between a minimum and a maximum, e.g. a volume.</summary>
public sealed class SliderSetting : SettingEntry<int>
{
    private readonly int _min;
    private readonly int _max;
    private readonly int _step;

    public SliderSetting(
        string labelKey,
        string descriptionKey,
        (int Min, int Max, int Step) range,
        Func<PlayerSettings, int> get,
        Func<PlayerSettings, int, PlayerSettings> set)
        : base(labelKey, descriptionKey, get, set)
    {
        (_min, _max, _step) = range;
    }

    public override MenuItem Item(PlayerSettings settings, Action<PlayerSettings> change) =>
        new SliderItem(Label, Get(settings), _min, _max, _step, value => change(Set(settings, value)), Enabled(settings))
        {
            Description = DescriptionFor(settings),
        };
}
