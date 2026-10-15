# Phase 6 — Online features & Steam integration (outline)

**Goal:** everything needed for a closed beta on Steam.
**Exit (from ROADMAP):** closed beta via Steam Playtest.

> Outline only. Several gaps were postponed to this phase; decide them first, then expand into full tasks.

## Decisions to make at phase start
- Quick match with a small player pool: ping/region filtering and fallback (GAP-39).
- Steam Deck: native Linux build vs Proton; suspend/resume during online matches; glyphs; on-screen keyboard (GAP-40).
- Release engineering: version scheme, branching, build signing, steamcmd secrets, rolling back a bad build (GAP-41).
- Crash reporting, telemetry and GDPR (GAP-42).

## Workstreams
- **P6-A Lobbies:** public/private lobby UI, quick match, rematch.
- **P6-B Replay browser.**
- **P6-C Steam features:** achievements, cloud saves, Steam Input with glyphs.
- **P6-D Steam Deck:** readable text at 1280×800, locked 60 fps, Verified checklist.
- **P6-E Release pipeline:** CI exports Windows + Linux and uploads to a Steam beta branch (SteamPipe/steamcmd).
- **P6-F Crash reporting** per the decision above.
