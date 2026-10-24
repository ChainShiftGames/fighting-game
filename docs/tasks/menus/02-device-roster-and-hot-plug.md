# UI-02 Device roster and controller hot-plug

> **Done:** merged in [PR 94](https://github.com/ChainShiftGames/fighting-game/pull/94) on 2026-10-23.
> Decisions: [ADR-0038](../../adr/0038-device-roster-and-assignment.md).
> Hot-plug was checked by hand in training with two controllers, one at a time. Not yet tried by hand: versus with a
> pad per player, and Start on a free pad taking a disconnected one's place (both covered by logic tests).

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), then [README.md](README.md) in this folder.
**Where:** `game/Input/`, the match startups in `game/Online/` and `game/Training/` (only the device wiring),
`game/Presentation/Events/JoypadRumbleMotor.cs` (slot → device), `tests/FightingGame.Tests/Input/`, a new ADR.
**Depends on:** nothing. Can run alongside UI-01.

## Goal

Today player N simply reads joypad slot N (`GamepadDevice(..., slot: player)` in `LocalMatchStartup`,
`TrainingMatchStartup`, `OnlineMatchStartup`), plus their own half of the keyboard. That makes "pick the controller
you want" impossible. Build a stable list of input devices and an explicit **player → devices** assignment that the
match startups use, and handle controllers connecting and disconnecting at any time.

## What to build

1. **Device roster.** Every usable input device with a stable identity and a display name: each joypad (from
   `JoypadRoster`) and the keyboard. Keep the **no-double-input rule** documented in `JoypadRoster` (Steam Input's
   virtual pad vs. the physical one): a physical controller appears exactly once.
   - Keyboard: today both players share it with two layouts (WASD and arrows, `default_bindings.jsonc`). Recommended:
     list it as **two devices**, "Keyboard (left)" and "Keyboard (right)", one per layout, so two people on one
     keyboard can still pick sides. Record the choice in the ADR.
   - A device keeps its identity across a quick unplug and replug where Godot allows it (same GUID/name), so a
     reconnecting controller goes back to its player.
2. **Device assignment.** A plain object saying which devices each player reads, for example `PlayerDevices`.
   Match startups build each player's `PlayerInputReader` from it instead of from slot numbers. Rumble follows
   the assignment too. Until UI-03 exists, the default assignment reproduces today's behaviour exactly.
3. **Hot-plug events.** Connect and disconnect events other code can subscribe to (side select uses them, menus show
   a notice).
4. **Disconnect during a match.**
   - Local versus or training: a player's assigned controller disconnecting **pauses the match** and shows "Controller
     disconnected — reconnect or press Start on another controller". Reconnecting the same controller, or pressing
     Start on a free one (which takes its place), carries on. Until UI-08's pause menu exists, a simple overlay is
     fine.
   - Online: the match can't pause. Just keep reading the reconnected controller, and show a small notice.
5. **Notices.** A small, reusable "toast" in a screen corner: "Controller connected: DualSense", "… disconnected".
   Presentation only; never covers the HUD gauges.

## Design notes

- The assignment is read **before** each logic frame like today; don't add a frame of delay (ARCHITECTURE.md §3).
  No allocation per frame in the input path.
- Assignment and roster logic in plain C#, tested without the engine (connect/disconnect order, keyboard halves,
  reconnect keeps the player, no device on two players).
- Bindings stay per player for now (`PlayerBindings`); per-device profiles are UI-10's job. Don't build them here.
- *Proposed* ADR: device identity, keyboard halves, assignment, disconnect behaviour.

## Done when

- Default behaviour is unchanged (same keys and pads drive the same players as before).
- A test assignment (e.g. P1 = second pad, P2 = keyboard left) drives the right players in versus and training.
- Unplugging a pad mid-versus pauses and the notice shows; replugging carries on.
- Logic tests for roster and assignment. Build, tests and format pass.

## Out of scope

The side select screen itself (UI-03), rebinding (UI-10), button icons (UI-11).
