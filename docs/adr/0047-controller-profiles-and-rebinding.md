# ADR-0047: Controller layouts per device, controls file, rebinding menu

- **Status:** Proposed (Oct 2026)

## Context

UI-10 ([brief](../tasks/menus/10-controls-and-rebinding.md)) adds full remapping. Bindings were per *player* in
`user://dev_bindings.jsonc`, edited by hand, while ADR-0038 §3 already said they would move to the device. A
player's hitbox layout must follow the hitbox to either side, and nobody may lose the bindings they have.

## Decision

1. **A layout belongs to a device** (`game/Input/Profiles/`, plain C#): `ControlProfiles` keeps the one keyboard's
   layout and each joypad's own by `InputDeviceId` (GUID + ordinal, ADR-0038 §2). A joypad without one uses the
   joypad default. The reader (`InputDeviceReader`) reads each device with its own layout whoever plays on it, so
   a layout follows its device across sides. Supersedes ADR-0038 §3. **Player settings.**
2. **One keyboard, one layout.** Default: WASD + U I O / J K L, P = special + C (player 1's old keys). Player 2's
   old arrows + numpad keys are the "Keyboard: arrows and numpad" preset. Nothing else read player 2's keyboard
   half: menus keep their own fixed keys (arrows, Enter, Escape), and `--p2-devices keyboard` (developer option)
   now reads the keyboard's one layout.
3. **What a layout holds:** game inputs (directions + every button of `data/input/buttons.json`, several controls
   each), menu confirm and back (keys and buttons only), and for pads left stick on/off, deadzone and trigger
   threshold. Menu directions, tabs and start stay the fixed `MenuBindings` mapping.
4. **Storage:** `user://controls.json`, its own file (layouts are device data, not `PlayerSettings` values), with
   `schemaVersion` 1, `keyboard`, `joypadDefault`, `joypads` (guid, ordinal, name, layout) and kept `presets`. Read
   leniently like the settings file: a bad part falls back with a warning; an unreadable file is copied to
   `controls.json.unreadable` before defaults replace it. Saved when the controls screens close.
5. **Migration, once:** with no controls file but a `dev_bindings.jsonc`, player 1's keys become the keyboard layout
   and player 1's pad bindings the joypad default; player 2's keyboard and pad layouts, where they differ from
   player 1's and every shipped preset, become loadable presets. The old file is left as it is and never read or
   created again (delete it whenever). A broken old file is not migrated: the old game used the defaults too.
6. **Shipped layouts** in `game/Input/default_bindings.jsonc` (embedded, now presets, checked by the tests): the
   first of each kind is the default, "Reset to default" goes back to it. Keyboard: WASD, arrows and numpad,
   leverless (keyboard mode, up on Space). Pad: classic, leverless (pad mode, d-pad, stick off).
7. **Resolved at match start:** `PlayerInputReader.All` compiles each connected device's layout
   (`InputDeviceReader.Resolve`); a frame only looks up what is built (a pad first seen mid-match is compiled once).
   No added latency, no per-frame allocation. Changes made from the pause menu count from the next match. Menus
   read confirm and back from the live layouts on each press.
8. **Lock-out fallback:** holding the default back control (Escape, or a pad's B) for 2 s is back on every menu
   screen whatever back is bound to (`MenuBindings.FallbackHoldMilliseconds`, **developer setting**).
9. **Rebinding screen** (`game/UI/Settings/Controls/`): the Controls tab lists every connected device; each opens
   buttons and directions, menu buttons, stick and triggers (pads), presets, reset. "Press a control" takes the
   next press of *that* device only (6 s, **developer setting**); a control already on another input of the same
   layer asks: only for the new input, keep it on both, or cancel. A test area shows the raw word and what the logic
   sees. The tab's "Reset to defaults" resets every connected device.
10. **SOCD stays the fixed logic rule** (ADR-0002): directions are rebindable for leverless controllers, opposite
   directions are both sent, and there is no SOCD option anywhere.

## Consequences

- Two identical pads can swap layouts across restarts when plugged in the other order (ADR-0038 §2 ids); a stabler
  id (Steam Input handle) would fix it.
- Button icons (UI-11) replace the Godot control names the screens show now.
- A new input in the button set is unbound in saved layouts until the player binds it; the shipped layouts must bind
  it (tested).
