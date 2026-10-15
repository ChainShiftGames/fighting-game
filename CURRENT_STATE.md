# Fighting Game — Master Plan & Decision Record

> Self-contained summary of every decision made during planning (Oct 2026).
> Hand this file to any agent as the starting context. Items marked **OPEN** are not decided yet;
> items marked *(proposal)* are the current default until someone overrides them.

---

## 1. Team & constraints

- **Two developers**, both **co-own the whole codebase** (no fixed area ownership); each reviews the other's PRs.
- Both are experienced **C# backend developers** and fairly experienced **Godot** developers.
- Some ability in music/SFX. **No 3D modelling, rigging or animation experience.** Plan to try Claude + Blender MCP.
- Time: plenty, **no fixed deadline**. Budget: decided case by case, when needed.
- Workflow: **Claude Code in cloud sessions** against the GitHub repo `ChainShiftGames/fighting-game`.
  Heavy human involvement: humans approve specs, plans and PRs, and playtest every milestone.
- **Playtesters from the fighting game community are available.**

## 2. Vision

- **Reference games:** Guilty Gear (Xrd / Strive), Granblue Fantasy Versus, Under Night In-Birth.
- Traditional **1v1 on a 2D plane**, anime "air-dasher" lineage. Not tag, not a platform fighter.
- **Core principle: game feel above all.** Movement, hitstop, screen shake etc. must be tuned until every hit feels great.
- **Art:** 3D models with a custom toon shader that reads as 2D (Guilty Gear Xrd style): cel shading,
  inverted-hull outlines, edited normals, animation on 2s/3s (stepped poses). 2D sprites for effects/UI blend in
  via the same colour treatment. Palette swaps via palette textures; alternate costumes are separate 3D models.
- **Audience:** both newcomers and the competitive FGC, via GBVS-style shortcut specials (see §3) — not a separate mode.
- **Platforms:** Windows + Linux, **Steam Deck Verified** target. Steam only. No consoles planned, but don't architect them out.
- **Monetization:** premium price (~€15–20), free online play, optional paid characters/cosmetics after launch. No F2P.
- **Launch type: full 1.0** (no Early Access), with a 4-character roster *(roster size is a proposal)*.

## 3. Game design decisions

### Match
- Fixed **60 fps** simulation. Best of 3 rounds, 99 s timer *(proposal)*. Wall-to-wall stage with corners.
- Hitstop is gameplay (lives in the sim). Screen shake, zoom, flashes, slowdown visuals live in presentation.

### Controls
- **Classic motion inputs** are the baseline (236, 214, 623, 41236, 63214, 22, 236236, 632146; charge inputs later).
- **Plus a GBVS-style special button** that performs specials without motion inputs, with a tunable trade-off
  (e.g. less damage or a cooldown). One shared scheme for everyone — **no separate "modern" control mode**.
- **Dedicated dash button.**
- **Attack buttons: A, B, C, D** (plus Special and Dash). Defined in data so the layout can change later (ADR-0010).
- Throw: forward/back + two buttons *(proposal)*, ~5f tech window.
- Input buffer and leniency windows are tunable. Negative edge / button priority later.
- **Full controller support:** remapping, SOCD cleaning (hitbox/leverless controllers), deadzones, input display.

### Movement
- Walk, dash (run or step per character), backdash with invulnerability, jump with prejump, high jump.
- Air options: double jump + air dash sharing a per-character air-action budget.
- "Negative penalty" for running away: available as a toggle.

### Offense
- **Gatlings** (per-character chain routes + jump cancels), normal → special → super cancels. **Yes.**
- **Counter hits** (+ punish counter on recovery). **Yes.**
- **Combo limits:** damage proration, hitstun decay, gravity scaling, OTG rules, all tunable. **Yes.**
- Wall bounce / ground bounce / wall splat: later.
- **Strive-style wall break: NO.**

### Defense
- Stand/crouch block, high/low/overhead, chip damage, tunable air-block rules.
- Meter-costing **pushback guard** (Faultless Defense / Shield style).
- **Instant block** (just-in-time block reward).
- **Burst** (combo breaker on its own gauge).
- Throw tech. Fixed wake-up timing with quick-rise / back-rise options.
- **No forward roll through the opponent.**

### Meter
- Tension-style meter 0–100, gained by attacking/moving forward. Supers cost 50%.
