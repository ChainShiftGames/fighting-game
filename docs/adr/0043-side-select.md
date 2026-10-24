# ADR-0043: Side select

- **Status:** Proposed (Oct 2026)

## Context

Task UI-03 ([brief](../tasks/menus/03-controller-side-select.md)) lets each person slide their device to player 1 or
player 2 before character select. ADR-0038 §5 already says how the choice reaches the match: a chosen
`PlayerDevices` from `PlayerDevicesChoice`, which never claims new joypads. Left open: what confirm in the middle
does, training's player 2, online, and how the choice is remembered.

## Decision

1. **Plain C# rules** (`DeviceSides`, `game/UI/SideSelect/`); `SideSelectScreen` maps menu presses to roster devices,
   `SideSelectView` only draws. Left/right move one column, a side holds one device, a refused move (taken side,
   dummy, edge, ready device) only bumps a counter the view shakes on. Confirm on a side readies; back un-readies,
   then returns to the middle, then leaves. The keyboard's Escape is its back. A pad's Start also readies. Held
   directions (repeats) are ignored.
2. **Confirm in the middle takes the first free side and readies at once.** So the common case is one press, even
   with the keyboard always listed next to one pad. A lone device (the keyboard with no pads) starts on player 1.
3. **Training: player 2 is the dummy.** Training matches read one player's devices (`TrainingMatchStartup`), so the
   P2 column says "Training dummy" and no device can enter it. A device on P2 driving the dummy would come with the
   training menus (UI-13).
4. **Online skips side select.** One player on this PC plays the side the lobby gives them (ADR-0042) with the
   default online assignment (keyboard + first pad, claims new pads); there is nothing to choose. The online route
   stays online screens → character select.
5. **Session memory = the chosen assignment.** `PlayerDevicesChoice.ChooseVersus` / `ChooseTraining` keep it and hand
   the same instance to every later match and to character select, so rematch and reselect keep it and a pad that
   took a disconnected one's place (ADR-0038 §6) stays in it. Side select starts from the chosen assignment of the
   same mode, else the other mode's. Precedence: chosen, then the command-line test assignment, then the default.
6. **Hot-plug**: the screen follows the roster each frame. A new device appears in the middle; a press from a device
   not on the screen only makes it appear. Unplugging frees the side; the same device plugged back while its side is
   still free returns there (not ready).
7. **Moves on** once every side the mode plays is ready for `AllReadyMilliseconds` (400), so a last back can undo
   it. This, the slide (120 ms) and the shake (220 ms) are **developer settings** in `SideSelectSettings.Default`.
   The animations only change the look; input counts from the press.
8. **Icons**: generic keyboard / gamepad / arcade stick drawn from shapes; sticks are told from pads by name
   (`DeviceIcons`). UI-11 replaces them.
9. **Pads that share a name are numbered** ("XInput Controller 1", "… 2") in screen order, so two pads of one
   model can be told apart; a name that is unique on the screen has no number.

No player settings.

## Consequences

- Local versus needs two devices on the sides: with only the keyboard, the screen asks for a controller for P2.
- A device in the middle can't drive character select or the match; only the chosen devices do.
- The chosen assignment is never saved across sessions (ids aren't stable across restarts, ADR-0038 §2).
