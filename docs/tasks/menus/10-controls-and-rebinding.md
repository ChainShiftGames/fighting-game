# UI-10 Controls: rebinding and controller profiles

> **Done:** merged in [PR 112](https://github.com/ChainShiftGames/fighting-game/pull/112) on 2026-10-24.
> Decisions: [ADR-0047](../../adr/0047-controller-profiles-and-rebinding.md) (one layout per device, the keyboard
> has one; `user://controls.json`; `dev_bindings.jsonc` migrated once, then no longer read; layouts compiled at match
> start, so pause-menu changes count from the next match).
> Checked in the editor: Controls tab with keyboard + pad, test area, capture, duplicate question, migrating a real
> dev bindings file. Not yet tried by hand: rebinding a pad and a leverless controller, a match with the new layout,
> surviving a restart, a layout following its pad across sides, remapped menu buttons and the 2 s fallback.
> Known issues: two identical pads can swap layouts across restarts if plugged in the other order; menus don't scroll.

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md) (§6: keybinds are player settings applied before the
input word), [ADR-0010](../../adr/0010-attack-button-layout.md) (buttons), [ADR-0020](../../adr/0020-special-button-shortcuts.md)
(special button), [ADR-0002](../../adr/0002-input-defaults.md) (SOCD is fixed), then [README.md](README.md).
**Where:** `game/Input/` (bindings per device/profile, saving), `game/UI/Settings/Controls/` (new),
`tests/FightingGame.Tests/Input/`.
**Depends on:** UI-01, UI-02 (device identity), UI-09 (settings menu with a Controls tab).

## Goal

Full remapping for every device, saved per controller, so a player's hitbox layout follows that hitbox. Today the
bindings are per *player* in `user://dev_bindings.jsonc` (copied from `default_bindings.jsonc`), edited by hand.

## What to build

- **Profiles per device type and device:** a binding profile for each controller (by the identity from UI-02) and for
  the keyboard. A new controller starts from the default profile for its kind (pad / keyboard). A player's
  profile follows their device whichever side they play on (side select, UI-03).
- **Rebinding screen:** list of the inputs from `data/input/buttons.json` (A, B, C, D, Special, Dash) plus directions
  for keyboards and leverless. Select one, press the new control. Several controls per input are allowed (today's
  format already allows lists). A control bound twice shows a warning and the player picks which one to keep.
- **Menu buttons** (confirm/back from UI-01) can be remapped too, with a safe fallback: holding the default back
  button for 2 s always leaves the screen, so nobody locks themselves out.
- **Presets:** a few layouts per device kind (e.g. "Pad: classic", "Leverless"), plus "Reset to default".
- **Stick options for pads:** left stick on/off, deadzone, trigger threshold (they exist in the JSON already).
- **Test area:** press buttons and see which inputs light up (reuse the button check from UI-08 if it exists).
- Save as versioned player-settings JSON in `user://`. Read the existing `dev_bindings.jsonc` once and migrate it, so
  nobody loses their bindings.

## Design notes

- Rebinding changes only how devices become the input word, before the word is made (§6). SOCD stays the fixed rule
  in the logic; **no** SOCD option in the menu.
- No added input latency, no per-frame allocation from looking up profiles: resolve the profile when the match starts.
- Profile storage and migration in plain C#, tested: round-trip, migration from the old file, duplicate detection,
  default for a new device.

## Done when

- Rebind on pad, keyboard and a leverless controller; the match uses the new layout; it survives a restart and
  follows the controller to the other side.
- The old dev bindings migrate. The lock-out fallback works.
- Build, tests and format pass. Screenshot of the rebinding screen.

## Out of scope

Button icons (UI-11), Steam Input configuration.
