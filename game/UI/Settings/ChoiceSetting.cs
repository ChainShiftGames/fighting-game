using System;
using System.Collections.Generic;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>A setting picked from a few values with left and right; a saved value not in the list shows the first.</summary>
public sealed class ChoiceSetting<T> : SettingEntry<T>
{
    private readonly IReadOnlyList<SettingChoice<T>> _choices;
    private readonly MenuText[] _labels;

    public ChoiceSetting(
        string labelKey,
        string descriptionKey,
        IReadOnlyList<SettingChoice<T>> choices,
        Func<PlayerSettings, T> get,
        Func<PlayerSettings, T, PlayerSettings> set)
        : base(labelKey, descriptionKey, get, set)
    {
        _choices = choices;
        _labels = new MenuText[choices.Count];
        for (int i = 0; i < choices.Count; i++)
        {
            _labels[i] = choices[i].Label;
        }
    }

    public override MenuItem Item(PlayerSettings settings, Action<PlayerSettings> change) =>
        new OptionItem(
            Label,
            _labels,
            Math.Max(0, IndexOf(Get(settings))),
            index => change(Set(settings, _choices[index].Value)),
            Enabled(settings))
        {
            Description = DescriptionFor(settings),
        };

    private int IndexOf(T value)
    {
        for (int i = 0; i < _choices.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_choices[i].Value, value))
            {
                return i;
            }
        }

        return -1;
    }
}
