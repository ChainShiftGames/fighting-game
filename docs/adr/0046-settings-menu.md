# ADR-0046: Settings menu: one entry per setting, appliers, versioned settings file

- **Status:** Proposed (Oct 2026)

## Context

UI-09 ([brief](../tasks/menus/09-settings-menu.md)) replaces the key-driven F12 overlay with an options menu for
player settings (ARCHITECTURE.md §6), reachable from the main menu and the pause menu. Adding a setting must not
mean editing a long switch, and the old unversioned `user://player_settings.json` must keep its values.

## Decision

1. **A setting is three small pieces:** its value in `PlayerSettings` (limits in `PlayerSettingLimits`, checked
   by `PlayerSettingsCheck`), one `ISettingEntry` in `UI/Settings/SettingsTabs` (kinds: `ToggleSetting`,
   `ChoiceSetting<T>`, `SliderSetting`, each with a get and a `with` set), and, when it acts on the engine, one
   `IPlayerSettingApplier` registered in `PlayerSettingAppliers`. Each applier skips work when its part is
   unchanged, so all of them run on every change.
2. **`CurrentPlayerSettings`** holds the live value: changes apply at once, the file is written only by `Save()`,
   called when the menu is left (also when the pause menu closes around it). F2 (vsync shortcut) saves at once.
3. **Risky video changes** (window mode, window size) set `NeedsKeeping`: the screen asks "Keep this?" with a
   countdown (`SettingsMenuTiming`, developer setting, 10 s) and reverts on timeout, back or leaving. "Reset to
   defaults" resets only the tab's entries, asks first (cursor on Cancel), and goes through the same keep question.
   Both questions are `SettingsScreen.Over`, put on top of the stack by `GameMenus` like the replay browser's.
4. **File versioning:** `schemaVersion` (now 2) is written first. A file without one is version 1 (vsync, input
   delay); names are unchanged, so it reads as is and new values get defaults. A newer file is read by the names
   this game knows, with a warning. Out-of-range values (and unknown enum names, via `LenientEnumConverter`) fall
   back one by one with a warning; only malformed JSON falls back entirely.
5. **F12 is gone.** The menu is reached from the main menu (Options) and the pause menu (Options, both local and
   online: nothing in it changes logic; the input delay applies from the next online match).
6. **Audio buses** Master / Music / Effects / Voice in `res://default_bus_layout.tres`; today's placeholder sounds
   play on Effects.
7. **Not added:** UI scale (the only engine-wide knob, content scale, also zooms the stage; a menu-only scale needs
   every view to honour it) and gameplay display options (presentation has no input display, damage numbers or
   HUD opacity hook yet). The Gameplay display, Controls (UI-10) and Accessibility (UI-12) tabs show a "coming" line.

## Consequences

- A new setting touches `PlayerSettings`, `PlayerSettingsCheck`, one line in `SettingsTabs` and maybe an applier.
- A future rename in the file adds an upgrade step in `PlayerSettingsJson.Read` and bumps `SchemaVersion`.
- The window opens at 1280×720 by default (was the project's 1152×648) until the player picks a size.
