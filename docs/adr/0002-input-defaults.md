# ADR-0002: Input defaults (SOCD and button buffer)

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-11

## Context

Hitbox/leverless controllers can hold opposite directions at the same time (SOCD), which must resolve
consistently. Buffering decides how early a button press may come and still count.

## Decision

1. **SOCD default:** left + right = neutral, up + down = up (the common tournament standard).
   It is a setting; other modes (e.g. last-input-priority) can be selected.
2. **Button buffer:** 2 frames by default, configurable.
3. **Motion-input window:** a motion (236, 623, …) must be completed within 12 frames by default; separate
   from the button buffer and adjustable per motion.
4. **SOCD mode is a player setting.** It's applied on the player's own machine before the input word is created,
   so it never needs syncing.
5. **Button buffer and motion window are developer settings** in configuration data, easy to tweak during
   development (no rebuild). They're part of the match rules compared in the online handshake (ADR-0005), so
   both players resolve inputs identically. (See ARCHITECTURE.md §6 for developer vs player settings.)
