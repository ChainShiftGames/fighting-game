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
2. Each module owns a **fixed-size state slice** in `MatchState` (declared at compile time, part of snapshots and
   the checksum). Modules never touch another module's slice.
3. Modules change core state only through **core requests** (e.g. request cancel, spend meter, spawn entity,
   apply freeze). They never write core state directly. The core validates and applies requests in a fixed order.
4. Modules are created once at match start; no allocations per frame.

**Core capabilities reserved from P1-03** (each proven by a scenario test with a test-only module)
- **Per-fighter time scale:** integer ratio with a sub-frame counter in state (e.g. Roman Cancel slowdown).
- **Global freeze with exemptions:** super flash, Roman Cancel freeze; chosen fighters/entities keep running.
- **Cancel from any state:** a cancel request with priority that the state machine honours.
- **Per-fighter hitstop override.**
- **Entity spawning:** projectiles and module objects through the entity pool (P1-21).

**Match rules**
5. `MatchRules` is **one blittable struct** holding every gameplay-affecting setting: input windows, timer, trade
   rules, enabled modules and every module's config.
6. The logic reads gameplay settings **only** from `MatchRules`; an architecture test blocks other config sources
   in `Sim`.
7. The match-rules hash covers the whole struct. A test changes each field in turn and asserts the hash changes,
   so no gameplay setting can be left out of the online check.
8. Match rules are **developer settings**: players can never change them (ADR-0005 §8).

## Consequences

- P1-20 builds this contract before the core pipeline; P3-01 only adds live toggling for development.
- Adding a capability to the core later needs an ADR update.
