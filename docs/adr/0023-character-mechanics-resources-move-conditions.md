# ADR-0023: Character mechanics, resource channels and move conditions

- **Status:** Proposed (Oct 2026). Lukas asked for all of it (see Context); details below are the implementer's
  proposal, to confirm while building. **Built:** all of it, with the details below (Oct 2026); waiting for both
  devs to accept.
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
   Built as: `IMechanicModule.IsCharacterMechanic`; 24 lineup slots + one mechanic slot per fighter
   (`ModuleStates.MechanicSlot`); a mechanic's gauge serves its channel for its owner only, so mirrors don't
   collide. First mechanic: `character_gauge`, a character's own gauge on a channel.
2. **Modules see characters read-only:** `ModuleContext.CharacterOf(fighter)`. Moves are found by role, not index:
   `character.json` `moveRoles: { "burst": "burst" }`, resolved once at match start (fixes `burstMove: 34`). A module
   lists the roles it needs (`IMechanicModule.MoveRoles`) and reads them with `ModuleContext.RoleMove(fighter, i)`.
   Loading rejects a role no registered module asks for, and a character missing a role an enabled module needs.
3. **Resource channels:** an `IMeterGauge` serves one channel (int 0–7 from its config, `ChannelOf`); `MeterOf(fighter,
   channel)` / `TrySpendMeter(fighter, amount, channel)` route to the gauge of that channel; channel 0 = the main
   meter (today's behaviour). Two enabled gauges on one channel fail loading and match start. Optional move `cost: { "channel", "amount" }`: the state machine skips a move that
   can't be paid (input falls through, like move lock) and spends on start.
4. **Fighter flags:** game-wide flag names in `data/rules/flags.json` (bit index = order, at most 32; per flag
   `clearedWhenHit`). `FighterState.Flags`. Set/cleared by a move on start (`setsFlags`, `clearsFlags`), by being
   hit (when marked), and by a module request (`FlagsRequest`). Modules resolve their own flag names at creation.
   Built as: a module lists its flags in `IMechanicModule.Flags` and reads their bits with `ModuleContext.FlagMask`,
   resolved at match start like roles; the cleared-when-hit bits are `MatchRules.FlagsClearedWhenHit`, applied to
   strikes, projectiles and throws.
5. **Move conditions:** `requires: { "flags": [...], "notFlags": [...] }` checked in `MoveSelection.CanStart`
   (stance-only, install-only, burnout locks). Stances = a flag a stance-entry move sets; installs = a flag a
   mechanic sets and times (`install` module). A move's `cost` is checked against the fighter's meter as the state
   machine step starts (`AvailableMeter`) and paid right after it; with no gauge on the channel the move can't
   start.
6. **Follow-ups:** a cancel window with `followUp: true` is always allowed by the core (no module question), so
   rekkas and target combos work with gatlings off; a move with `followUpOnly: true` never starts from neutral.
   A follow-up window names its moves in `into` only, and a follow-up-only move is never reached by category.

## Consequences

- Move schema 3 (requires, cost, setsFlags…) and character schema 2 (mechanic, moveRoles) with
  `tools/DataMigrate` steps (ADR-0021); burst's config moves from an index to the `burst` role.
- A test character with a rekka chain, a stance and an install mechanic proves it, with scenarios:
  `mechanics_dummy` (`tests/Scenarios/MoveConditions/`, `tests/Scenarios/Install/`).
