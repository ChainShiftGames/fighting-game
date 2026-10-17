# ADR-0020: Special button shortcuts are core input, with a match-rules trade-off

- **Status:** Proposed (Oct 2026): decided by Lukas, second dev to approve
- **Gaps:** CURRENT_STATE §3 Controls (GBVS-style special button); P3-19

## Context

CURRENT_STATE §3 wants a GBVS-style special button that performs specials without motion inputs, with a tunable
trade-off, and one shared scheme (no "modern" mode). P3-19 listed it as a module, but which direction gives which
special is per-character data, and modules can't read character data (ADR-0011). Lukas decided it belongs in the
core as an alternative to motions.

## Decision

1. **A move may name a shortcut direction in its data:** `input.shortcut` (one numpad digit, e.g. `"6"`) in the move
   file. That direction held as the **special button** is pressed starts the move, like its motion + button would
   (same button buffer, stance and move priority rules; the direction is read on the press frame).
2. **The special button is input data:** `movementInputs.specialButton` in `data/input/motions.json` names a button
   of the button set (next to `dashButton`). Without it, shortcuts never trigger. **Developer setting.**
3. **The trade-off is match rules** (`data/rules/match.json`, **developer settings**, in `MatchRules` and the rules
   hash): `shortcutDamagePercent` (hits and chip of a shortcut move deal this percentage, after the counter hit
   bonus) and `shortcutCooldownFrames` (fighter frames after a shortcut move starts before the next shortcut works;
   motions are never locked). Either can be switched off (100 %, 0 frames).
4. **When both were input, the motion wins**, so the move gets no trade-off.
5. The fighter state records it (`FighterState.IsShortcutMove`, `ShortcutCooldownLeft`), so it is part of snapshots
   and the checksum. Any other way into a move (module cancel requests) is never a shortcut.

## Consequences

- Characters opt in per move in data; the editor edits `input.shortcut` through the schema form.
- Projectiles a shortcut move spawns carry the flag (`EntityState.IsShortcut`) and take the same damage trade-off;
  module spawns never do.
- P3-19 is core work, not a module: no module folder, no removal test.
