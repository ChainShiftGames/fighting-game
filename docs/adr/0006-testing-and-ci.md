# ADR-0006: Testing and CI

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-9, GAP-15, GAP-22

## Decision

1. **Minimal CI.** Every PR: build, game-logic tests, determinism test (SyncTest) and the logic speed benchmark
   (ADR-0003), on Linux. Windows build runs nightly. No UI/visual tests.
2. **Scenario tests** are plain text that humans and AI agents can write, e.g. "P1 does 5A point blank → P2 is
   hit on frame 5 and in hitstun for 12 frames".
3. **Golden replays:** recorded matches whose checksums must not change unless a change is intentional
   (then they're re-recorded in the same PR).
4. **Bot soak test** (two bots, 1 hour, simulated lag/loss, seeded inputs; fails on desync) runs **on demand**
   via a manual CI trigger, mainly to test the netcode infrastructure. Not scheduled nightly.
