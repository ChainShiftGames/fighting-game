# ADR-0022: Multi-hit moves, launches and juggles are core hit data

- **Status:** Accepted (Oct 2026): Lukas's call (fix the gap, keep the tuning choices open)
- **Gaps:** foundation review before Phase 4 (one hit per move, air hits always end in a knockdown)

## Context

Schema v1 gives a move one hit, and an airborne defender simply falls into a knockdown with the momentum it had.
Real characters need multi-hit normals and supers, launchers, juggles, bounces and OTG hits. Like throws and
counter hits (ADR-0017), these are fundamentals: they are core hit data, not modules. Which juggle *limits* a game
uses (proration, gravity scaling, OTG caps, air recovery) stays a tuning choice: modules or match rules.

## Decision

1. **A move has a list of hits** (`hits` in move schema v2, migrated from `hit`, ADR-0021). With more than one hit,
   each names its frames (`firstFrame`, `lastFrame`): ranges in order, not overlapping, inside the active frames.
   A hit box belongs to the hit whose frames it is active on; every hit has a hit or throw box, and every hit box
   frame belongs to a hit. A single hit without frames covers the whole active range (as v1). At most 32 hits.
2. **Each hit connects once.** `FighterState.HitsConnected` has a bit per hit of the current move; detection only
   looks at the hit of the current move frame. Hit, block and clash spend that hit; later hits can still connect.
   Cancel windows still read the move's outcome (`on: hit/block/whiff`).
3. **Optional reaction data per hit** (all default to v1 behaviour):
   - `launch` `{x, y}`: on hit, the defender takes this velocity (x away from the attacker, y up) and is airborne
     in air hitstun. `airLaunch` overrides it when the defender was already airborne. Without either, a grounded
     hit uses its `reaction` and an airborne defender keeps its momentum.
   - `groundBounce` `{y}`: the first landing of that juggle bounces up at `y` instead of knocking down.
   - `wallBounce` `{x, y}`: the first wall touch of that juggle sends the defender back off the wall.
   - `knockdownFrames`: how long the knockdown this hit causes lasts (default: the knockdown move's length);
     soft and hard knockdowns are data values. A quick rise/tech, if wanted, is a module.
   - `otg`: the hit can connect with a knocked-down defender (who needs hurtboxes in its knockdown move); other
     hits can't. An OTG hit without a launch knocks down again. Combo limits' OTG cap still applies.
4. **Air recovery is a match rule** (`airRecovery` in `data/rules/match.json`, **developer setting**): off, air
   hitstun lasts until landing (v1); on, a fighter whose hitstun ends in the air recovers into a free jump.
5. Projectiles use the same hit data, so their launches and bounces work the same way. Multi-hit projectiles
   (`hits` count with a rehit interval) are left for later; the projectile hit keeps one connection.

## Consequences

- New fighter state fields are part of snapshots and the checksum; golden replays re-record for the data hash only.
- Frame data shown in training still uses the move's overall active range; gaps between hits show as active.
