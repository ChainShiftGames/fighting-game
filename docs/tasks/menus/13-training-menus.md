# UI-13 Training mode menus

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), [ADR-0007](../../adr/0007-character-data-and-editor.md)
(frame meter, savestates), then [README.md](README.md) in this folder.
**Where:** `game/Training/` (menu screens and their entries), `tests/FightingGame.Tests/Training/`.
**Depends on:** UI-01. Uses UI-08's command list and UI-11's icons if they exist.

## Goal

The training features already work (frame meter, dummy modes, record/playback, savestates, restart; see
`game/Training/`). Their menu (`TrainingMenu`, entries implementing `ITrainingMenuEntry`) is a placeholder. Move it
onto the menu framework and make it quick to use mid-session, the way modern training modes are.

## What to build

- **Training menu on the framework**, grouped into pages: Dummy (mode, guard, recovery), Recording (slot, record,
  playback style), Display (frame meter, hitboxes if a player-facing toggle exists, input history), Position (reset
  presets). Keep every existing entry working; each stays its own small class.
- **Quick actions without opening the menu,** on buttons that don't clash with gameplay (e.g. Select/Back + button):
  reset position (centre, left corner, right corner, swap sides), start/stop recording, play back, save/load state.
  Show them in a small hint bar. Make the combos rebindable later (note it, don't build it).
- **Reset positions:** centre, P1 in left corner, P1 in right corner, swap sides, chosen with direction + reset.
  Positions are training settings, not match rules; check how restart already sets up the match.
- **Input history** panel per side (numpad or arrows + buttons, frames held), readable at a glance.
- **Settings remembered** between sessions (training settings already save via `TrainingSettingsStore`; extend it,
  keeping its versioning).

## Design notes

- Training menu entries stay pluggable: a new option is a new entry class plus one registration line.
- Nothing here may change logic results of a normal match; training-only options act through the training session.
- Tests for the entry logic and position presets (plain C#).

## Done when

- Every existing training feature is reachable from the new menu on a gamepad.
- Quick reset to each position and quick record/playback work without opening the menu.
- Build, tests and format pass. Screenshots of each page.

## Out of scope

New training features (combo trials, frame data overlay beyond the meter), CPU difficulty.
