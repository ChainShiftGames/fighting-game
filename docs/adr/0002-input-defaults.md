# ADR-0002: Input rules (SOCD and button buffer)

- **Status:** Accepted (Oct 2026; SOCD revised Oct 2026)
- **Gaps:** ROADMAP GAP-11

## Context

Hitbox/leverless controllers can hold opposite directions at the same time (SOCD), which must resolve
consistently. Buffering decides how early a button press may come and still count.

## Decision

1. **SOCD is a fixed input-priority rule, not a setting:** left + right = neutral, up + down = up.
2. It's applied **inside the game logic's input step**. Devices send raw directions (the 4 direction bits can carry
   left + right together), so local play, online, replays and bots all use exactly one rule. Changing it needs a
   code change and an ADR.
3. **Button buffer:** 2 frames by default. **Motion-input window:** a motion (236, 623, …) must be completed within
   12 frames by default; separate from the button buffer and adjustable per motion.
4. **Button buffer and motion windows are developer settings** in configuration data, easy to tweak during
   development (no rebuild). They live in `MatchRules` (ADR-0011) and are compared in the online handshake, so
   both players resolve inputs identically.
