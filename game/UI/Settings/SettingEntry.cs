using System;
using System.Collections.Generic;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>
/// What every kind of <see cref="ISettingEntry"/> shares: its label and description, and reading and writing its one
/// <see cref="PlayerSettings"/> value.
/// </summary>
/// <typeparam name="T">The value's type.</typeparam>
public abstract class SettingEntry<T> : ISettingEntry
{
    private readonly Func<PlayerSettings, T> _get;
    private readonly Func<PlayerSettings, T, PlayerSettings> _set;

    /// <param name="set">The settings with the value replaced, e.g. <c>(s, on) => s with { Vsync = on }</c>.</param>
    protected SettingEntry(
        string labelKey, string descriptionKey, Func<PlayerSettings, T> get, Func<PlayerSettings, T, PlayerSettings> set)
    {
        Label = MenuText.Key(labelKey);
        Description = descriptionKey.Length == 0 ? MenuText.Empty : MenuText.Key(descriptionKey);
        _get = get;
        _set = set;
    }

    public bool NeedsKeeping { get; init; }

    /// <summary>When it can be changed, e.g. the window size only while windowed; always when null.</summary>
    public Func<PlayerSettings, bool>? IsEnabled { get; init; }

    /// <summary>The line shown instead of the description while it can't be changed.</summary>
    public string DisabledDescriptionKey { get; init; } = string.Empty;

    protected MenuText Label { get; }

    protected MenuText Description { get; }

    public abstract MenuItem Item(PlayerSettings settings, Action<PlayerSettings> change);

    public PlayerSettings Reset(PlayerSettings settings, PlayerSettings defaults) => _set(settings, _get(defaults));

    public bool IsSame(PlayerSettings first, PlayerSettings second) =>
        EqualityComparer<T>.Default.Equals(_get(first), _get(second));

    protected T Get(PlayerSettings settings) => _get(settings);

    protected PlayerSettings Set(PlayerSettings settings, T value) => _set(settings, value);

    protected bool Enabled(PlayerSettings settings) => IsEnabled?.Invoke(settings) ?? true;

    protected MenuText DescriptionFor(PlayerSettings settings) =>
        Enabled(settings) || DisabledDescriptionKey.Length == 0 ? Description : MenuText.Key(DisabledDescriptionKey);
}
