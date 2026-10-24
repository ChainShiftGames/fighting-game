# UI-09 Settings menu

> **Done:** merged in [PR 109](https://github.com/ChainShiftGames/fighting-game/pull/109) on 2026-10-24.
> Decisions: [ADR-0046](../../adr/0046-settings-menu.md) (a setting = value + one `SettingsTabs` entry + optional
> applier; applied at once, saved on leaving; window mode/size "Keep this?" 10 s; settings file `schemaVersion` 2;
> F12 overlay gone). Not built: UI scale, gameplay display options (nothing in presentation to switch yet).
> Checked in the editor's embedded window with the keyboard: every tab, the keep countdown reverting, the reset
> question, saving on back. Not yet tried by hand: gamepad, settings surviving a restart, hearing the volumes, window
> mode/size in the standalone game. Known issue: the column is centred, so the tab picker moves between short and
> long tabs (needs a `MenuView` change).

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md) (§6 settings audiences),
[ADR-0004](../../adr/0004-game-loop-and-camera.md) (vsync), [ADR-0005](../../adr/0005-online-architecture.md) §4
(input delay), then [README.md](README.md) in this folder.
**Where:** `game/UI/Settings/` (new), `game/Settings/` (more player settings and their JSON), replaces
`game/UI/OptionsMenu.cs` (the F12 overlay), `tests/FightingGame.Tests/Settings/`.
**Depends on:** UI-01 (widgets: picker, toggle, slider).

## Goal

A real options menu for **player settings only**, reachable from the main menu and the pause menu. It replaces the
key-driven F12 overlay. Controls get their own task (UI-10); accessibility options are UI-12 but live in this menu.

## Tabs

- **Video:** window mode (windowed / borderless fullscreen / fullscreen), resolution (windowed only), vsync (with
  a one-line note on its latency cost, ADR-0004), UI scale. The 60 Hz logic rate is not a setting.
- **Audio:** master, music, effects, voice volumes. Wire them to Godot audio buses; create the buses if they don't exist.
- **Gameplay display:** input display on/off, damage numbers, HUD opacity. Only things presentation already has or can
  read; don't add logic.
- **Online:** input delay: automatic or a fixed number (today `InputDelayOption`, `InputDelaySettings` limits).
- **Language:** English only for now, but the option exists and switches the translation locale (UI-01).
- **Accessibility:** tab exists, filled by UI-12.
- **Controls:** tab exists, filled by UI-10.

## Behaviour

- Changes preview immediately where safe (volume, UI scale). Risky video changes (resolution, fullscreen) ask "Keep
  this? (10 s)" and revert on timeout.
- Saved when leaving the menu, to the versioned JSON in `user://` (`PlayerSettingsStore`). Add a schema version if
  there isn't one, and load older files without losing values. Steam Cloud sync of this file comes later.
- "Reset to defaults" per tab, with confirmation.
- Tab left/right switches tabs (menu actions from UI-01).

## Design notes

- Each setting is a small piece: its value in `PlayerSettings`, its widget, how it's applied. Adding a setting means
  adding one of these, not editing a long switch.
- Nothing here may change logic results (§6). If a setting you're asked to add would, stop and raise it.
- Tests: settings JSON round-trip, defaults, versioning, invalid values fall back with a warning.

## Done when

- Every setting above can be changed with a gamepad, applies, and survives a restart.
- The F12 overlay is gone (or kept as a dev shortcut that opens this menu).
- Build, tests and format pass. Screenshot of each tab.

## Out of scope

Rebinding (UI-10), accessibility options (UI-12), Steam Cloud.
