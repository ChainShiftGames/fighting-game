# ADR-0011: Module contract, core capabilities and match rules

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-24 (contract part; the lock policy is ADR-0005 §8)

## Context

ARCHITECTURE.md §5 requires mechanics to be removable modules. The core pipeline (P1-03) must know how modules plug
in before it's built, and it must already support what Roman Cancel and the open signature mechanic will need.

## Decision

**Module contract**
1. A module implements `IMechanicModule`, with optional hooks at fixed pipeline points: after input, before/after
   state machines, on hit, on block, on throw, end of frame. Hook order between modules is a list in data.
   - **Air guard question** (P1-24, ADR-0017): `IAirGuardHook` lets a module answer whether an airborne defender
     holding back may block an attack. The core asks only then, blocks when any module says yes, and does the rest
     (air blockstun, chip). A question, not a hook point: no state changes or requests in it.
   - **Cancel window question** (P3-10): `ICancelWindowHook` decides whether a fighter may take a cancel its
     move's data window offers (into a move or a jump). With no module saying yes, data windows never cancel.
   - **Hit adjustment question** (P3-12): `IHitAdjustmentHook` adjusts a hit's damage, hitstun and hitstop before
     it lands, or makes it whiff; modules answer in slot order, each adjusting the previous result.
2. Each module owns a **fixed-size state slice** in `MatchState` (declared at compile time, part of snapshots and
   the checksum). Modules never touch another module's slice.
3. Modules change core state only through **core requests** (e.g. request cancel, spend meter, spawn entity,
   apply freeze). They never write core state directly. The core validates and applies requests in a fixed order.
4. Modules are created once at match start; no allocations per frame.

**Core capabilities reserved from P1-03** (each proven by a scenario test with a test-only module)
- **Per-fighter time scale:** integer ratio with a sub-frame counter in state (e.g. Roman Cancel slowdown).
- **Global freeze with exemptions:** super flash, Roman Cancel freeze; chosen fighters/entities keep running.
- **Cancel from any state:** a cancel request with priority that the state machine honours; its target is a move or
  (P3-18) `CancelRequest.Neutral`, which makes the fighter free at once, a buffered press starting its move.
- **Per-fighter hitstop override.**
- **Entity spawning:** projectiles and module objects through the entity pool (P1-21).
- **Per-fighter gravity scale** (P3-12): extra gravity in thousandths, kept until the next request (combo limits).
- **Per-fighter stun override** (P3-16): replace remaining hitstun/blockstun (e.g. instant block).
- **Module events** (P3-16): a module asks the core to emit a `FrameEventKind.Module` event for presentation; its
  detail packs the module id and the module's own code.
- **Move lock** (P3-13, proposed): a request that stops a fighter starting moves of given categories for one state
  machine step; the state machine reports the category of an attack it started (`FighterState.StartedMoveCategory`).
- **Pushback scale** (P3-15, proposed): a request that scales what is left of a fighter's pushback slide (core
  pushback: `Sim.Collision.PushbackStage`); scales from several modules multiply.
- **Meter gauge** (P3-13, proposed): `IMeterGauge` module role; any module reads/spends meter via
  `ModuleContext.MeterOf` / `TrySpendMeter`, routed by the core to the enabled gauge.

**Match rules**
5. `MatchRules` is **one blittable struct** holding every gameplay-affecting setting: input windows, timer, trade
   rules, enabled modules and every module's config.
6. The logic reads gameplay settings **only** from `MatchRules`; an architecture test blocks other config sources
   in `Sim`.
7. The match-rules hash covers the whole struct. A test changes each field in turn and asserts the hash changes,
   so no gameplay setting can be left out of the online check.
8. Match rules are **developer settings**: players can never change them (ADR-0005 §8).

**Module lineup in data** (P3-01)
9. Modules are registered in code in one place, `Sim.Modules.RegisteredModules` (one line each), and switched in
   data: `data/rules/modules.json` lists module names in hook order; each listed module has
   `data/rules/modules/<name>.json` with `enabled` and every key of its config. A module declares those keys and
   their ranges as its `ConfigLayout` (in its own folder), so Sim.Data reads any module's file without naming it.
10. Enabled modules fill lineup slots in hook order; disabled ones take none and their config stays out of
    `MatchRules` (it is in the data hash). Unknown names, unlisted files and missing keys fail loading.
11. Data is reloaded for every match, so a module switched in a dev build applies when the match restarts, with a
    new rules hash. Exported builds read the embedded data (ADR-0015), so they have one fixed lineup.
12. Scenario tests ignore the data lineup and enable only the modules they list, so dev toggles can't change them.
    How to add a module: `src/Sim/Modules/README.md`.

## Consequences

- P1-20 builds this contract before the core pipeline; P3-01 only adds toggling for development (§9–12).
- Removing a module also means deleting its data file and its `modules.json` entry, or data loading fails.
- Adding a capability to the core later needs an ADR update.
