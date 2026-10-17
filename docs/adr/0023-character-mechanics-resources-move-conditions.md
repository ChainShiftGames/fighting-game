# ADR-0023: Character mechanics, resource channels and move conditions

- **Status:** Proposed (Oct 2026): design sketch, not built yet. Lukas asked for all of it (see Context); details
  below are the implementer's proposal, to confirm while building.
- **Gaps:** foundation review before Phase 4 (no character-unique mechanics, one meter, no rekkas/stances/installs)

## Context

Lukas: characters must be able to have unique mechanics; the architecture must allow several meter types; rekkas,
follow-ups, stance-only moves and install moves must all be possible for a character. Today modules come only from
the global lineup (16 slots, 10 used), never read character data, find moves by index (`burst.json` has
`"burstMove": 34`), only the first `IMeterGauge` is asked (`ModuleLineup.CreateMeterAccess`), stances are only
ground/air, move locks are by category, and cancel windows only work when the gatlings module allows them.

## Proposed decision

1. **Character mechanics are modules scoped to one fighter.** A registered module can be character-scoped; a
   character names at most one in `character.json` (`mechanic: { "name": …, <config keys> }`, read through the
   module's `ConfigLayout`, part of the data hash, not of `MatchRules`). The lineup gives each fighter's mechanic a
   reserved slot after the systemic modules; `ModuleContext.Owner` says which fighter it serves (-1 = match-wide).
   Same hooks, requests, events and removal test as other modules. Raise `ModuleStates.MaxModules` (e.g. 24 + 2).
2. **Modules see characters read-only:** `ModuleContext.CharacterOf(fighter)`. Moves are found by role, not index:
   `character.json` `moveRoles: { "burst": "burst" }`, resolved once at match start (fixes `burstMove: 34`).
3. **Resource channels:** an `IMeterGauge` serves one channel (int 0–7 from its config); `MeterOf(fighter,
   channel)` / `TrySpendMeter(fighter, amount, channel)` route to the gauge of that channel; channel 0 = the main
   meter (today's behaviour). Optional move `cost: { "channel", "amount" }`: the state machine skips a move that
   can't be paid (input falls through, like move lock) and spends on start.
4. **Fighter flags:** game-wide flag names in `data/rules/flags.json` (bit index = order, at most 32; per flag
   `clearedWhenHit`). `FighterState.Flags`. Set/cleared by a move on start (`setsFlags`, `clearsFlags`), by being
   hit (when marked), and by a module request (`FlagsRequest`). Modules resolve their own flag names at creation.
5. **Move conditions:** `requires: { "flags": [...], "notFlags": [...] }` checked in `MoveSelection.CanStart`
   (stance-only, install-only, burnout locks). Stances = a flag a stance-entry move sets; installs = a flag a
   mechanic sets and times.
6. **Follow-ups:** a cancel window with `followUp: true` is always allowed by the core (no module question), so
   rekkas and target combos work with gatlings off; a move with `followUpOnly: true` never starts from neutral.

## Consequences

- Move schema 3 (requires, cost, setsFlags…) and character schema 2 (mechanic, moveRoles) with
  `tools/DataMigrate` steps (ADR-0021); burst's config moves from an index to the `burst` role.
- A test character with a rekka chain, a stance and an install mechanic proves it, with scenarios.
