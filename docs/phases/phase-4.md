# Phase 4 — Vertical slice (outline)

**Goal:** one real character, stage and full presentation that genuinely feel great online.
**Exit (from ROADMAP):** online mirror match that genuinely feels great; FGC playtest done and both devs say it passes.

> Outline only. Expand into full tasks (phase-0 format) when Phase 3 is nearly done.

## Foundation pass (before Phase 4, Lukas's call after the Oct 2026 review)
Content model (this order; ADR-0021–0023):
- [x] Data format changes through `tools/DataMigrate`; external tools read an export (ADR-0021; export: todo)
- [x] Multi-hit moves, launches, juggles, ground/wall bounce, knockdown length, OTG; air recovery rule (ADR-0022)
- [x] Character-unique mechanics and several meter types (ADR-0023): character mechanic modules, move roles,
  resource channels; move `cost` comes with move conditions
- [x] Move conditions: rekkas/follow-ups, stance-only and install moves, move costs, fighter flags (ADR-0023)
- [ ] `tools/DataExport` for external character tools (ADR-0021)
Netcode hardening (ADR-0024–0025) and presentation hardening (ADR-0026) run in their own branches.

## Decisions to make at phase start
- Spike B pass criteria (GAP-7 proposals: ≤ ~1 day per animation, VRoid commercial-use terms checked).
- Hit-effect implementation (GAP-32).
- Whether to keep source art out of git or start using LFS/another store (GAP-34 revisit).

## Workstreams
- **P4-A Spike B (art):** VRoid → Blender → Godot, 5 stepped animations, one palette swap; go/no-go ADR
  (no-go → outsourcing plan).
- **P4-B Toon rendering:** fixed light per character, hard shading, constant-width outlines, 4× MSAA, palette
  textures. Every effect is a **developer setting** toggle (ADR-0008).
- **P4-C Character 1:** model, animations driven by the logic frame number, boxes re-authored in the editor.
- **P4-D Stage 1:** stage art, walls at the logic's positions.
- **P4-E Hit effects and VFX:** driven by frame-tagged events, rollback-safe (P2-08).
- **P4-F Audio:** Godot audio buses, SFX per event, −16 LUFS, **directional audio** panned by on-screen position.
- **P4-G HUD:** health, meter, burst, timer, combo counter.
- **P4-H Game feel pass:** hitstop, shake, camera zoom, all values as **developer settings**.
- **P4-I Playtest:** external FGC session; pass when both devs say so.
- **P4-J Steam page:** partner account, app fee, store art, trailer, "Coming Soon" *(timing proposal)*.
