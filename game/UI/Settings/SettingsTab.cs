using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>One tab of the options menu: its settings, and a line under its name (what is still to come); may be empty.</summary>
public sealed record SettingsTab(MenuText Name, IReadOnlyList<ISettingEntry> Entries, MenuText Note);
