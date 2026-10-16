# ADR-0017: Throws and counter hits are core; air block is a module

- **Status:** Proposed (Oct 2026; Lukas approved 2026-10-16, co-dev reviews in the PR)

## Context

ARCHITECTURE.md §5 makes every mechanic a removable module, and Phase 3 planned counter hits (P3-11) and throws
with tech (P3-14) as modules. Both are fundamentals of every modern fighting game rather than variants to
experiment with:
- **Throws** complete strike/throw/block: without them blocking has no answer. They reach into the state machine
  (thrower and thrown states), hit detection (throw boxes, P1-10), invulnerability rules (stun, wake-up, landing)
  and same-frame exchanges. As a module they would need a contract larger than the feature (moving fighters,
  forcing states, throw-only invulnerability).
- **Counter hits** change the hit itself (hitstun, hitstop, damage) from the defender's move phase, before the
  reaction is applied. A module only sees a hit after it resolved (`IOnHitHook`) and would need hitstun and damage
  overrides added to the contract.
- Both change `MatchState`, character data and golden replays. Phase 2's handshake, desync tools and replays build
  on those, so they land before Phase 2.

Air block is different: rules vary between games (none, partial, full) and the devs may want none or only some.

## Decision

1. **Counter hits and punish counters are core and always present.** A hit during the defender's attack startup or
   active frames is a counter hit; during its recovery a punish counter. The extra hitstun, hitstop and damage come
   from move hit data, with defaults as **developer settings** in `MatchRules` (`data/rules/match.json`).
2. **Throws and throw tech are core and always present.** Throws are character moves (category `throw`, buttons
   through the button set, ADR-0010) connecting with throw boxes against hurtboxes. The tech window and throw
   invulnerability frames are **developer settings** in `MatchRules`. Modules react through `IOnThrowHook`
   (ADR-0011).
3. **Air block is a removable module** under ADR-0011. Off means airborne fighters can't block. If the contract
   can't reach the blocking decision, the core gets the smallest extension it needs (an interface or hook the core
   defines, recorded in ADR-0011); the core never special-cases the module.
4. **The exception is narrow.** Only these two are core. Every other mechanic stays a module (§5); making another
   one core needs its own ADR.
5. **Built in Phase 1:** P1-22 (counter hits), P1-23 (throws and tech), P1-24 (air block); P3-11 and P3-14 are
   dropped.

## Consequences

- No switch can remove throws or counter hits, but every value stays tunable: zero extras make a counter hit a
  normal hit with an event, and a character without throw moves can't throw.
- Fewer module combinations to test; the scenarios and SyncTest always cover both.
- `MatchState`, `MatchRules`, character data and golden replays change in P1-22 and P1-23 (new data and rules
  hashes).
- Air block adds a core extension only if P1-24 shows the contract can't reach blocking.
