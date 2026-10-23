# ADR-0039: Menu framework: screen stack, menu actions, theme and translation keys

- **Status:** Proposed (Oct 2026)

## Context

UI-01 ([brief](../tasks/menus/01-menu-framework.md)): every later menu task needs a shared base. The placeholder
`MenuPanel` had one column of buttons, no back stack, no focus memory, and confirm/back hard-coded per device.

## Decision

1. **Plain C# core, Godot drawing.** `game/UI/Framework/` holds the stack, focus, navigation, input mapping, repeat,
   grace and text formatting with no engine calls (tested in `tests/FightingGame.Tests/UI/`). Godot nodes only read
   events (`MenuInput`) and draw (`MenuView`, `game/UI/Widgets/`).
2. **Screens are `IMenuScreen` classes** (`game/UI/Screens/`): title, body lines, items, `Back()`, `Press()`. Read every
   displayed frame, so they can change while shown. A new screen is one class plus one line that pushes it.
3. **`MenuStack`**: push, pop, replace, and `Show(path)` for state-driven flows (keeps screens already in place).
   Each entry owns its `MenuFocus`, so back restores the cursor; focus is by position, so relabelled items keep it.
   Back asks the top screen; it pops only when the screen agrees and isn't the bottom one.
4. **Items + widgets.** Items (`ButtonItem`, `OptionItem`, `ToggleItem`, `SliderItem`, `HeaderItem`) hold state and
   react to actions; one widget each draws them. `MenuWidgets.For` is the one registration point. Widgets take no
   Godot focus; the highlight is a theme style, so Godot's built-in `ui_*` navigation never fights ours.
5. **Menu actions, not keys.** `MenuAction` flags (directions, confirm, back, tab left/right, start), each press
   tagged with its `MenuDevice` (keyboard, a joypad by Godot index, mouse). Default mapping in
   `MenuBindings.Default`: arrows, Enter/Space, Escape (= back + start), Backspace, Q/E; pad d-pad/left stick, A, B,
   shoulders, Start. Only joypads in a `JoypadRoster` slot are read (no-double-input rule). Rebinding: UI-10.
6. **Input from unhandled events**, so overlays above the menus (options, training) still take theirs first; held
   state is re-checked against the devices each frame. Directions repeat while held.
7. **Developer settings** in `MenuTiming.Default`: input grace 600 ms (after menus open from closed), repeat delay
   350 ms, interval 90 ms, transition fade 120 ms (0 = off; input goes to the new screen at once), stick deadzone 0.5.
8. **Theme** `game/Assets/UI/menu_theme.tres`: Marcellus (as the HUD) with **M PLUS 1p** as fallback for Japanese
   (`MPLUS1p-Regular.ttf`, SIL OFL 1.1, commercial use allowed, licence beside it). Colours, sizes, focus highlight
   and backdrops are theme type variations (`MenuItem`, `MenuItemFocused`, `MenuTitle`…).
9. **Translation keys.** Menu text is `MenuText`: a key into `game/Assets/UI/menu_text.csv` (Godot CSV import,
   registered in `project.godot`; English only) with arguments, or `MenuText.Plain` for words the game didn't write
   (names, error messages). A test checks every `MENU_`/`LOBBY_` key in `game/UI/` exists in the table.

## Consequences

- Later screens (side select, character select, settings…) are new `IMenuScreen` classes; custom-drawn screens will
  need their own view next to `MenuView` (decide in the task that needs one).
- Screens that need both players read `MenuPress.Device`; mapping devices to players is UI-02/UI-03.
- Not yet translated: `MatchStatusText` and lobby notices from `Platform.Steam` (shown as plain text), the F12
  options overlay and the training menu (UI-09, UI-13).
