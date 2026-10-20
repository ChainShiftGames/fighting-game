# ADR-0020: Special button shortcuts are core input, mapped in the motion set

- **Status:** Accepted (Oct 2026): approved by Lukas with changes (mapping in the motion set, not per move)
- **Gaps:** CURRENT_STATE §3 Controls (GBVS-style special button); P3-19

## Context

CURRENT_STATE §3 wants a GBVS-style special button that performs specials without motion inputs, with a tunable
trade-off, and one shared scheme (no "modern" mode). P3-19 listed it as a module, but which press gives which special
depends on the moves' inputs, which modules can't read (ADR-0011). It belongs in the core as an alternative to
motions. A first draft named a shortcut direction per move; Lukas chose one GBVS-style mapping for every character.

## Decision

1. **The motion set maps motions to shortcut directions:** a motion in `data/input/motions.json` may name a
   `shortcut` (one numpad digit): 236 → `"5"`, 623 → `"6"`, 214 → `"4"`. Each direction maps at most one motion.
   **Developer setting**, part of the data hash.
2. **The special button is input data:** `movementInputs.specialButton` names a button of the button set. Without
   it, shortcuts never trigger. **Developer setting.**
3. **Trigger:** a move whose motion has a shortcut starts when the special button and the move's own button(s) are
   pressed together within the button buffer (`ButtonPress.IsUnusedPressAt(history, special | move buttons, f)`, ADR-0033) while
   the shortcut direction is held on that frame `f`. 5 + special + C gives 236C; 5 + special + A gives 236A. Stance,
   move lock and cooldown rules apply as for motions.
4. **Priority** (free moves and cancels): a move input by a real motion (more than one step, e.g. 236C) beats the
   shortcut, and gets no trade-off; the shortcut beats moves with a single-direction motion (5C, 2C, throws) that
   the same press also matches. With no shortcut input, the character's move order decides as before.
5. **The trade-off is match rules** (`data/rules/match.json`, **developer settings**, in `MatchRules` and the rules
   hash): `shortcutDamagePercent` (hits and chip of a shortcut move deal this percentage, after the counter hit
   bonus) and `shortcutCooldownFrames` (fighter frames after a shortcut move starts before the next shortcut works;
   motions are never locked). Both are off for now (100 %, 0 frames); the code stays so a penalty can be tried.
6. The fighter state records it (`FighterState.IsShortcutMove`, `ShortcutCooldownLeft`), so it is part of snapshots
   and the checksum. Any other way into a move (module cancel requests) is never a shortcut.

## Consequences

- Every character gets the same shortcuts from its motions; move files have no shortcut field.
- Projectiles a shortcut move spawns carry the flag (`EntityState.IsShortcut`) and take the same damage trade-off;
  module spawns never do.
- P3-19 is core work, not a module: no module folder, no removal test.
