# UI-01 Menu framework

> **Done:** merged in [PR 95](https://github.com/ChainShiftGames/fighting-game/pull/95) on 2026-10-23.
> Decisions: [ADR-0039](../../adr/0039-menu-framework.md).

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), then [README.md](README.md) in this folder.
**Where:** `game/UI/` (new framework, plus porting the existing screens), `game/Assets/UI/` (theme, fonts),
`tests/FightingGame.Tests/UI/`, a new ADR. Small wiring changes in `game/Main.cs` only.
**Depends on:** nothing. This is the foundation every other menu task builds on.

## Goal

Replace the single placeholder `MenuPanel` with a small, reusable menu system that every later screen (side select,
character select, settings, pause, results…) is built on. Today each screen is a list of `MenuEntry` buttons picked by
`MenuScreenChoice`; there is no back stack and no focus memory, and confirm and back are hard-coded per device.

## What to build

1. **Screen stack.** Push, pop and replace screens. Back pops; the bottom screen decides whether back does anything.
   A screen shown over a match (pause, results) and a full-screen one (main menu) are both supported.
2. **Focus memory.** Going back restores the item that was focused on that screen. Rebuilding a screen whose labels
   changed keeps focus where it was (today's `MenuPanel.Rebuild` already does this for one screen; keep that).
3. **Menu actions, not keys.** One small set of menu actions (up, down, left, right, confirm, back, tab left/right,
   start) read from every device: keyboard, every gamepad and leverless controllers that report as a pad or keyboard.
   Each action reports **which device** produced it, so screens like side select and character select can tell the
   players apart. The default mapping is code with one obvious place to change it; task 10 later lets players rebind
   menu buttons too.
   - Held directions repeat after a delay (developer setting), so long lists scroll.
   - Keep the **input grace** after a menu opens (today `InputGraceMilliseconds`), so mashing at the end of a fight
     never picks something.
4. **Reusable widgets.** Button/list item, left-right option picker (for settings: "Vsync < On >"), a toggle, a slider
   and a section header, all keyboard/pad driven, all styled by the theme. Small and composable; no catch-all class.
5. **Shared theme.** One Godot `Theme` resource in `game/Assets/UI/` (fonts, sizes, colours, focus highlight) used by
   every screen. Use a font that has Japanese characters, so adding languages later costs nothing (check its licence
   allows commercial use, and say which font in the ADR).
6. **Translation groundwork.** All menu text goes through Godot translation keys (a CSV translation table in
   `game/Assets/UI/`, English only for now). The framework makes using a key the easy path.
7. **Screen transitions.** A short fade or slide between screens. Its length is a developer setting, 0 turns it off.
   It must never delay input: a press during a transition goes to the new screen.
8. **Port the existing screens** (main menu, online lobby, match over, pause) onto the framework, with the same
   behaviour as today. `MenuPanel` / `MenuEntry` / `MenuScreenChoice` can then go or shrink. Task 04 builds the
   real flow, so no new screens here.

## Design notes

- Keep the stack, navigation and focus logic in plain C# with no Godot types (README rule), and test it in
  `tests/FightingGame.Tests/UI/`: push/pop/back, focus restore, repeat timing, grace period.
- A screen is something like `IMenuScreen` (one-line summary, small interface). Screens are added by writing a new
  class, not by adding cases to a switch.
- No `MenuManager`. Split by job: the stack, the action reader, the theme, each widget.
- Write a *Proposed* ADR covering the stack, the menu actions with device identity, the theme and translation keys.

## Done when

- The four existing screens work exactly as before on keyboard and gamepad, with back and focus memory.
- A new screen can be added in one file plus one line that opens it.
- Logic tests cover the stack, focus and input repeat/grace. Build, tests and format pass.
- Screenshots (Godot MCP, if available) of the ported screens using the shared theme.

## Out of scope

New screens, the controller side select (UI-03), rebinding (UI-10), button icons (UI-11).
