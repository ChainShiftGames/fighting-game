# ADR-0031: Hit sparks: looks derived from hit data, per-hit overrides in the art file

- **Status:** Proposed (Oct 2026): awaiting both devs
- **Gaps:** ROADMAP GAP-7 (art pipeline)

## Context

The placeholder spark was 8 rays drawn in 2D, the same for every hit. The target is Guilty Gear Strive: light hits
burst, heavy hits and specials tear along the attack's direction, through and behind the defender, and every hit
feels as strong as it is. That needs per-hit look data, which hit of a multi-hit move landed, and detail that varies
from hit to hit but survives rollback.

## Decision

1. **Hit events carry the hit.** `Hit`, `Block` and `Clash` details pack the move and its hit index
   (`HitEventDetail`); a first hit packs to the move alone, so single-hit moves keep their old detail and golden
   replay checksums.
2. **Looks come from hit data by default** (`HitSparkLooks`): weight from hitstop (8 → 0, 20 → 1), plus a bit on
   counter hit, less on block; angle from the launch (forward without one); tear for specials, supers and weight
   ≥ 0.5, burst for the rest, blocks and throws. Most hits need no art settings.
3. **Per-hit overrides live in the character art file** (ADR-0028), a `sparks` section by move ID with one entry per
   hit: `angle` (degrees from the attacker's facing, 90 up), `offset` (pixels, forward and up), `style`
   (`burst`/`tear`), `scale`. Not in `data/`, for ADR-0028 §3's reasons: no data hash change, no schema bump.
4. **Drawn by one shader** (`Assets/Effects/hit_spark.gdshader`) on a card turned to the attack's angle, over the
   fighters. Its debris and impact lines are computed from age and a seed hashed from the event id, never Godot
   particles: same look on every re-show, exact under rollback and hitstop. Tones: hit, block, clash, counter.
5. **A counter hit marks its hit's spark** instead of adding one. A spark first shown late (a rollback found its
   hit) still shows its first drawing once, then the logic frame's age. The attacker's facing is kept from the first
   showing.
6. **Tuned in the VFX Lab** (ADR-0029) as an impact part: `VfxLab/Sheets/hit_spark/`; the Lab's impact kinds show
   the tones (fizzle = counter).

## Consequences

- Character-unique sparks (a different shader per character or move) are a later step: add an effect name to the
  `sparks` entry when the first one needs it.
- Not done yet: screen shake along the spark's angle and scaled by weight; projectile sparks use the projectile's
  hit data but have no per-projectile settings.
