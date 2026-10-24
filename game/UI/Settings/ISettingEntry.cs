using System;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>One player setting in a settings tab: the menu item that changes it and how it goes back to its default.</summary>
public interface ISettingEntry
{
    /// <summary>A change must be confirmed ("Keep this?") or it reverts: a video change that could leave no picture.</summary>
    bool NeedsKeeping { get; }

    /// <param name="change">Gets the settings with this entry's new value.</param>
    MenuItem Item(PlayerSettings settings, Action<PlayerSettings> change);

    /// <summary><paramref name="settings"/> with this entry's value taken from <paramref name="defaults"/>.</summary>
    PlayerSettings Reset(PlayerSettings settings, PlayerSettings defaults);

    bool IsSame(PlayerSettings first, PlayerSettings second);
}
