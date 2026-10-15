# ADR-0003: Determinism rules and game-logic speed budget

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-3, GAP-4, GAP-29

## Context

Rollback only works if both PCs compute exactly the same result from the same inputs, and if the game can
re-run several frames of logic within one displayed frame. Floating-point maths, unordered collections and
hidden randomness can make two PCs drift apart.

## Decision

1. **Integers only** in the game logic. Positions and velocities are in subpixels (1 px = 1000 units).
   Any trigonometry/square roots use our own integer helpers or lookup tables.
2. **Banned in the sim assembly:** `float`, `double`, `decimal`, `System.Random`, `DateTime`/`Stopwatch`,
   `Dictionary`/`HashSet` iteration, LINQ, `async`, mutable static state. Randomness comes from a seeded PRNG
   stored in the game state.
3. The ban is enforced by `Microsoft.CodeAnalysis.BannedApiAnalyzers` (plus a check for float types) as build
   errors, so violations fail CI regardless of who wrote the code.
4. **Rollback window:** 8 frames by default, configurable.
5. **Speed budget:** one logic frame (both characters, all mechanics) takes **≤ 1 ms on a Steam Deck** and
   allocates no memory. This is the project's only performance budget; there are no graphics budgets.

## Consequences

- Slightly more verbose maths code; no physics engine in gameplay.
- The speed budget is measured by a benchmark in CI and checked at each milestone on a real Deck.
