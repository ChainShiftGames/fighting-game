# ADR-0038: Device roster, player device assignment and controller hot-plug

- **Status:** Proposed (Oct 2026)

## Context

Player N read joypad slot N plus their own keyboard half, so nobody could pick a controller, and unplugging one pad
shifted the others onto the wrong player. Side select (UI-03) needs a device list and an explicit assignment; task
UI-02 ([brief](../tasks/menus/02-device-roster-and-hot-plug.md)) builds them.

## Decision

1. **Device roster** (`DeviceRoster`, `game/Input/`): the keyboard as **two devices**, "Keyboard (left)" and
   "Keyboard (right)", always listed, then every joypad `JoypadRoster` uses, lowest Godot index first. The
   no-double-input rule stays in `JoypadRoster`: a physical pad Steam Input re-presents is never listed.
2. **Joypad identity** (`InputDeviceId`) = Godot's GUID (the model) + ordinal among connected pads of that model. A
   pad keeps its id while connected (same Godot index and GUID); a new pad takes the lowest free ordinal of its
   model. So a quick unplug and replug gets the same id back, and two identical pads keep their own ids when one of
   them is unplugged. Not stable across restarts or when both identical pads are swapped while unplugged; nothing
   stores ids yet.
3. **Keyboard halves keep their layout**: the left half reads the first bindings' keyboard keys (WASD), the right
   half the second's (arrows, number pad), whichever player plays on it. A joypad reads with the reading player's
   joypad bindings. Bindings stay per player until UI-10.
4. **Assignment** (`PlayerDevices`): per local player, a list of device ids; a device is on at most one player and
   stays assigned while disconnected. Each match start builds one from `PlayerDevicesChoice`; the readers and rumble
   read it before every logic frame, so changes count from the next frame, no allocation.
5. **Default assignment** (`DefaultPlayerDevices`) reproduces the old behaviour: versus P1 = left half + 1st pad,
   P2 = right half + 2nd pad; training = left half + 1st pad; online = both halves + 1st pad. It also *claims new
   joypads*: a pad that connects unassigned replaces a waiting player's missing pad, else goes to the first player
   without one. A chosen assignment (side select, or the command line) never claims.
6. **Disconnect during a match** (`ControllerReconnect`, `ControllerDisconnectOverlay`): local versus and training
   hold the match (`FrameStepping.IsWaitingForController`) and say "controller disconnected — reconnect or press
   Start on another controller" while a player's assigned pad is gone. Reconnecting it, or Start on a free pad
   (which takes its place), carries on. Online never pauses: the reconnected pad is simply read again.
7. **Notices**: `DeviceRoster.Connected` / `Disconnected` events; `ControllerNotices` shows each as a toast
   (`ToastNotice`, top-right below the HUD gauges).
8. **Test assignment**: `--p1-devices` / `--p2-devices <list>` (`keyboard-left`, `keyboard-right`, `pad1`, `pad2`,
   …) — **developer option** on the command line, until side select exists. Invalid options are logged and the
   default is used.

No new settings for players; the toast's timing is a developer setting in code.

## Consequences

- UI-03 builds a `PlayerDevices` from side select and hands it to the match startups through `PlayerDevicesChoice`.
- UI-08's pause menu can replace the simple disconnect overlay; the hold stays separate from the pause menu's.
- Saving a player's controller across sessions would need a stabler id (e.g. Steam Input's controller handle).
