using System;
using System.Collections.Generic;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>
/// The options menu: one tab of player settings at a time (tab left/right, or the picker at the top), each change
/// applied at once. Risky video changes ask "Keep this?" and revert unless kept in time; "Reset to defaults" asks
/// first. Both questions are <see cref="Over"/> this screen. Leaving saves.
/// </summary>
public sealed class SettingsScreen : IMenuScreen
{
    private const long MillisecondsPerSecond = 1000;

    private readonly IReadOnlyList<SettingsTab> _tabs;
    private readonly MenuText[] _tabNames;
    private readonly CurrentPlayerSettings _settings;
    private readonly SettingsMenuTiming _timing;
    private readonly Func<long> _now;
    private readonly Action _close;
    private PlayerSettings? _beforeKeeping;
    private long _keepUntil;

    /// <param name="now">The menu clock in milliseconds, for the "Keep this?" countdown.</param>
    /// <param name="isOverMatch">Opened from the pause menu, over the match.</param>
    /// <param name="close">Leaves the menu (back to the main or pause menu), after it has saved.</param>
    public SettingsScreen(
        IReadOnlyList<SettingsTab> tabs,
        CurrentPlayerSettings settings,
        SettingsMenuTiming timing,
        Func<long> now,
        bool isOverMatch,
        Action close)
    {
        _tabs = tabs;
        _settings = settings;
        _timing = timing;
        _now = now;
        IsOverMatch = isOverMatch;
        _close = close;
        _tabNames = new MenuText[tabs.Count];
        for (int i = 0; i < tabs.Count; i++)
        {
            _tabNames[i] = tabs[i].Name;
        }
    }

    public MenuText Title { get; } = MenuText.Key("MENU_TITLE_OPTIONS");

    public IReadOnlyList<MenuText> Body => Tab.Note.IsEmpty
        ? [MenuText.Key("MENU_SETTINGS_TABS_HINT")]
        : [MenuText.Key("MENU_SETTINGS_TABS_HINT"), MenuText.Empty, Tab.Note];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            PlayerSettings settings = _settings.Value;
            var items = new List<MenuItem> { new OptionItem(MenuText.Key("MENU_SETTINGS_TAB"), _tabNames, TabIndex, Show) };
            foreach (ISettingEntry entry in Tab.Entries)
            {
                items.Add(entry.Item(settings, next => Change(next, entry.NeedsKeeping && !entry.IsSame(settings, next))));
            }

            if (Tab.Entries.Count > 0)
            {
                items.Add(new ButtonItem(MenuText.Key("MENU_SETTINGS_RESET"), AskToReset)
                {
                    Description = MenuText.Key("MENU_SETTINGS_RESET_INFO", Tab.Name),
                });
            }

            return items;
        }
    }

    public bool IsOverMatch { get; }

    public int TabIndex { get; private set; }

    /// <summary>The question asked over this screen ("Keep this?", "Reset?"), or null.</summary>
    public IMenuScreen? Over { get; private set; }

    private SettingsTab Tab => _tabs[TabIndex];

    public bool Back()
    {
        Leave();
        _close();
        return false;
    }

    public void Press(MenuPress press)
    {
        if (press.Has(MenuAction.TabLeft) || press.Has(MenuAction.TabRight))
        {
            int step = press.Has(MenuAction.TabLeft) ? -1 : 1;
            Show((TabIndex + step + _tabs.Count) % _tabs.Count);
        }
    }

    /// <summary>Reverts an unkept video change once its time is up; call once per displayed frame while open.</summary>
    public void Follow()
    {
        if (_beforeKeeping is not null && _now() >= _keepUntil)
        {
            Revert();
        }
    }

    /// <summary>The menu closes (also when the pause menu closes around it): an unkept change reverts, then it saves.</summary>
    public void Leave()
    {
        Revert();
        Over = null;
        _settings.Save();
    }

    private void Show(int tab) => TabIndex = tab;

    private void Change(PlayerSettings next, bool needsKeeping)
    {
        if (needsKeeping)
        {
            _beforeKeeping ??= _settings.Value;
            _keepUntil = _now() + _timing.KeepMilliseconds;
            Over = new KeepSettingScreen(SecondsLeft, Keep, Revert, IsOverMatch);
        }

        _settings.Change(next);
    }

    private int SecondsLeft()
    {
        long left = Math.Max(0, _keepUntil - _now());
        return (int)((left + MillisecondsPerSecond - 1) / MillisecondsPerSecond);
    }

    private void Keep()
    {
        _beforeKeeping = null;
        Over = null;
    }

    private void Revert()
    {
        if (_beforeKeeping is { } before)
        {
            _beforeKeeping = null;
            _settings.Change(before);
        }

        Over = null;
    }

    private void AskToReset() => Over = new ResetTabScreen(Tab.Name, ResetTab, () => Over = null, IsOverMatch);

    private void ResetTab()
    {
        Over = null;
        var defaults = new PlayerSettings();
        PlayerSettings current = _settings.Value;
        PlayerSettings next = current;
        bool needsKeeping = false;
        foreach (ISettingEntry entry in Tab.Entries)
        {
            next = entry.Reset(next, defaults);
            needsKeeping |= entry.NeedsKeeping && !entry.IsSame(current, next);
        }

        Change(next, needsKeeping);
    }
}
