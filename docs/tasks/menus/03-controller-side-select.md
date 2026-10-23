# UI-03 Controller side select

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), then [README.md](README.md) in this folder.
**Where:** `game/UI/SideSelect/` (new), `tests/FightingGame.Tests/UI/SideSelect/`. A one-line hook into the flow.
**Depends on:** UI-01 (menu framework, menu actions with device identity) and UI-02 (device roster, assignment,
hot-plug events).

## Goal

Before any mode starts and before character select, show every connected controller and let each person slide theirs
to the **P1** or **P2** side. Whoever is on a side plays that side; controllers left in the middle sit out. This is
how players choose which controller they play with, every time, without digging into settings.

## How it works

```
   P1 SIDE              NOT PLAYING              P2 SIDE
 ┌──────────┐      ┌──────────────────┐      ┌──────────┐
 │ [DualSense]│ ◀── │ [Xbox pad]       │ ──▶ │ [Hitbox] │
 │  READY ✓ │      │ [Keyboard left]  │      │          │
 └──────────┘      │ [Keyboard right] │      └──────────┘
                   └──────────────────┘
```

- Every device from the roster (UI-02) is an icon with its name, starting in the middle column.
- **Left / right** on a device moves *that device's* icon one column (middle → P1 side, P2 side → middle, …).
  A side holds one device. Moving into a taken side does nothing (small shake or sound).
- **Confirm** on a device that is on a side **readies** it. Back un-readies it, then moves it back to the middle. Back
  on a device in the middle (or the keyboard's Escape) leaves the screen.
- Any press on a device not on the screen yet (a controller just plugged in) makes it appear. Unplugging removes it and
  frees its side (hot-plug events from UI-02).
- **Moves on** to character select once every required side is ready:
  - Versus: both sides.
  - Training: P1 side only. A device on P2 is allowed and later controls the dummy (if training supports it; otherwise
    show P2 as "dummy").
  - Online: exactly one local side, the one the player is on in the match (show only that side, or both with the
    remote one locked).
- **Quick path:** when only one device is connected in versus or training, it is placed on P1 already and one confirm
  continues. Don't add a step for the common case.
- **Remembers** the last assignment for this session: coming back (rematch → character select, or a new versus)
  starts with the same devices on the same sides, so two players don't redo it every game.

## Design notes

- The screen's state (columns, readiness, required sides per mode, rejoining) is plain C#, fully tested without the
  engine. The Godot scene only draws it and forwards menu actions with their device.
- Output: the UI-02 `PlayerDevices` assignment, handed to the next screen and on to the match startup.
- Device icons: a generic pad / keyboard / arcade-stick picture for now. UI-11 swaps in per-brand ones.
- Animate an icon sliding between columns. Keep it short and never block input.
- Every text through translation keys (UI-01).

## Done when

- Two pads and a keyboard: each can go to either side, ready up and continue; the match uses exactly those devices.
- Training needs only P1. The single-device quick path works.
- Plug and unplug on this screen behave as above. The assignment is remembered for the session.
- Logic tests for every rule above. Build, tests and format pass. Screenshots of the screen with 3 devices.

## Out of scope

Character select itself (UI-05). Per-device bindings (UI-10).
