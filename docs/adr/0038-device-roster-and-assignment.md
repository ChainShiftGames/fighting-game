# ADR-0038: Device roster, player device assignment and controller hot-plug

- **Status:** Accepted (Oct 2026)

## Context

Player N read joypad slot N plus their own keyboard half, so nobody could pick a controller, and unplugging one pad
shifted the others onto the wrong player. Side select (UI-03) needs a device list and an explicit assignment; task
UI-02 ([brief](../tasks/menus/02-device-roster-and-hot-plug.md)) builds them.

## Decision

1. **Device roster** (`DeviceRoster`, `game/Input/`): **one keyboard device**, "Keyboard", always listed, then
   every joypad `JoypadRoster` uses, lowest Godot index first. The no-double-input rule stays in `JoypadRoster`: a
   physical pad Steam Input re-presents is never listed. Two players sharing one keyboard is not supported.
2. **Joypad identity** (`InputDeviceId`) = Godot's GUID (the model) + ordinal among connected pads of that model. A
   pad keeps its id while connected (same Godot index and GUID); a new pad takes the lowest free ordinal of its
   model. So a quick unplug and replug gets the same id back, and two identical pads keep their own ids when one of
   them is unplugged. Not stable across restarts or when both identical pads are swapped while unplugged; nothing
   stores ids yet.
3. **Bindings follow the player**: the keyboard reads with the keyboard keys of whoever plays on it, a joypad with
   their joypad bindings. Bindings stay per player until per-device profiles (UI-10).
4. **Assignment** (`PlayerDevices`): per local player, a list of device ids; a device is on at most one player and
   stays assigned while disconnected. Each match start builds one from `PlayerDevicesChoice`; the readers and rumble
   read it before every logic frame, so changes count from the next frame, no allocation.
5. **Default assignment** (`DefaultPlayerDevices`): player 1 gets the keyboard and the 1st pad, player n the n-th pad
   (versus, training and online alike). It also *claims new joypads*: a pad that connects unassigned replaces a
   waiting player's missing pad, else goes to the first player without one. A chosen assignment (side select, or
   the command line) never claims.
6. **Disconnect during a match** (`ControllerReconnect`, `ControllerDisconnectOverlay`): local versus and training
   hold the match (`FrameStepping.IsWaitingForController`) and say "controller disconnected — reconnect or press
   Start on another controller" while a player's assigned pad is gone. Reconnecting it, or Start on a free pad
   (which takes its place), carries on. Online never pauses: the reconnected pad is simply read again.
7. **Notices**: `DeviceRoster.Connected` / `Disconnected` events; `ControllerNotices` shows each as a toast
   (`ToastNotice`, top-right below the HUD gauges).
8. **Test assignment**: `--p1-devices` / `--p2-devices <list>` (`keyboard`, `pad1`, `pad2`, …) — **developer
   option** on the command line. Invalid options are logged and the default is used. Since UI-03, an assignment
   chosen on side select takes precedence ([ADR-0043](0043-side-select.md)).

No new settings for players; the toast's timing is a developer setting in code.

## Consequences

- Local versus without pads has no device for player 2: two players need at least one pad.
- UI-03 builds a `PlayerDevices` from side select and hands it to the match startups through `PlayerDevicesChoice`.
- UI-08's pause menu can replace the simple disconnect overlay; the hold stays separate from the pause menu's.
- Saving a player's controller across sessions would need a stabler id (e.g. Steam Input's controller handle).
