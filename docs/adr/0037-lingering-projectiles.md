# ADR-0037: Lingering projectiles (rehit interval)

- **Status:** Proposed (Oct 2026)

## Context

P1-21 made every projectile land once and disappear on hit or block. Testman's 214A/B/C leg sweep ignites the floor:
a fire that stays where it was lit and keeps putting a blocking opponent in blockstun. Traps, floor fires and
lingering pillars are common in fighting games, and they are data, not a character-unique mechanic.

## Decision

1. **A projectile's data may have `rehitInterval`** (frames, default 0). 0 keeps P1-21's rule: gone on its first hit
   or block. Above 0 it stays out after landing and can't touch again for that many frames; its lifetime, a clash
   (durability) or the round reset still ends it. A stationary one is just `velocity.x` 0.
2. **State:** each entity carries `RehitCooldown`, counted down on the frames the entity advances (it stops in a
   global freeze like the rest of the entity); while above 0 the entity touches nothing but still clashes.
3. **Same-frame exchange under the clash rule:** a lingering projectile doesn't land that frame but isn't removed
   (a fireball in the exchange still is).
4. Every landing emits the usual projectile hit/block event; presentation plays the impact each time.

## Consequences

- `ProjectileData` and `EntityState` grow by one int: the data hash and snapshot size change once.
- The training bot treats a projectile that doesn't fly forward as reaching only as far as its boxes.
- Multi-hit flying projectiles (a fireball that hits 3 times) are not covered: this is one landing per interval,
  not a hit count. Add a count if a move needs it.
