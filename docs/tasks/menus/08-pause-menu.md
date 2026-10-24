# UI-08 Pause menu, command list and button check

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), then [README.md](README.md) in this folder.
**Where:** `game/UI/Pause/` (new), replaces the pause screen ported in UI-01, `tests/FightingGame.Tests/UI/`.
**Depends on:** UI-01. Settings entry opens UI-09 when it exists.

## Goal

The in-match menu players expect: resume, look up moves, check buttons, change settings, leave. Training already has
its own menu (`game/Training/TrainingMenu*.cs`); in training, the pause menu adds a "Training settings" entry that
opens it, rather than duplicating it.

## What to build

- **Who paused:** Start on any device that plays in the match opens it. Only that player's devices control it (shown
  as "P1 paused"); the other player can't close it or pick for them.
- **Entries:** Resume · Command list · Button check · Settings · (Training: Training settings, Reset position) ·
  Character select · Main menu. Leaving asks for confirmation.
- **Command list:** that player's character's moves, read from data: name, input in numpad notation and icons
  (236 + C → "↓↘→ + C"), notes like "air OK" or "special button shortcut" (ADR-0020). Grouped: normals, command
  normals, specials, supers, throws. A new move in data appears automatically. Moves without a display name show
  their id.
- **Button check:** shows that player's buttons lighting up as they press them, using their real bindings, so they
  can find a leverless layout without leaving the match. Reuse `InputWordText` / `InputReadout` where it helps.
- **Online:** the match can't pause (the other PC keeps running). The menu opens over the running match with the same
  entries except reset, and leaving warns that it counts as a disconnect.

## Design notes

- Keep the move-list formatting (motion → arrows/words, grouping) in plain C#, tested without the engine, separate
  from the screen. It's reused later by training and the command list overlay.
- Holding the match (local) uses the existing frame stepping; it must not change logic results or replays.

## Done when

- Pause on either player's controller in versus; only that player controls it. Resume, leave with confirmation.
- Command list matches testman's moves in data. Button check reflects rebinding.
- Online pause doesn't stop the match.
- Logic tests for the move-list formatting. Build, tests and format pass. Screenshots.

## Out of scope

Settings screen content (UI-09), training features (UI-13), frame data in the command list.
