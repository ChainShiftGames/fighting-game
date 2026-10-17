# Mechanics modules

This folder holds the module contract (namespace `Sim.Modules`); each mechanic lives in `<Name>/`
(`Sim.Modules.<Name>`). Rules: ARCHITECTURE.md §5, [ADR-0011](../../../docs/adr/0011-module-contract.md).
Example to copy: `AirBlock/`.

## Adding a module

1. **Code** in `src/Sim/Modules/<Name>/` only:
   - `<Name>Module : IMechanicModule` plus the hook interfaces it needs. Pick an unused non-zero `Id` and a
     snake_case `Name`.
   - Optional `<Name>State : IModuleState` and `<Name>Config : IModuleConfig` (int fields only; layout tests check
     the size).
   - With config: `ConfigLayout = ModuleConfigLayout.For<<Name>Config>(new ModuleConfigField("key", min, max), …)`,
     one field per config int in declaration order. Keys are camelCase; `enabled` is reserved.
   - Needs a character's move (e.g. burst)? List role names in `MoveRoles` and find the move with
     `context.RoleMove(fighter, roleIndex)`; characters name it in `character.json` `moveRoles` (ADR-0023). Never
     store a move index in config. `context.CharacterOf(fighter)` reads the rest of the character.
   - Keeps a resource? Implement `IMeterGauge` and take the channel from config (`ChannelOf`); others reach it with
     `context.MeterOf(fighter, channel)` (Meter/README.md).
2. **Register** with one line in `RegisteredModules.cs`, kept alphabetical.
3. **Data** (developer settings):
   - `data/rules/modules/<name>.json`: `schemaVersion`, `enabled`, and every config key
     (schema: `data/schema/module.schema.json`).
   - Add the name to `order` in `data/rules/modules.json` at the spot where its hooks should run.
   - Ship it `"enabled": false` unless the task says otherwise. Enabling it changes gameplay: re-record the golden
     replays and say why in the PR. Any data change changes the data hash, so re-record anyway
     (`GOLDEN_REPLAYS_RECORD=1`, see `tests/Netcode.Tests/GoldenReplays`).
4. **Tests**: scenarios in `tests/Scenarios/<Name>/` turn it on with `setup: module <name> [config…]`; they never
   see the data lineup. Unit tests go in `tests/Sim.Tests/Modules/<Name>/`.

## Character mechanics (ADR-0023)

A mechanic only some characters have (installs, a stance gauge, ammo) is a module with `IsCharacterMechanic => true`.
Example: `CharacterGauge/`.
- No `data/rules/modules/` file and no `order` entry: a character switches it on in its `character.json`,
  `"mechanic": { "name": "<name>", <every config key> }` (at most one per character; part of the data hash).
- It runs in its owner's own slot after the lineup (`ModuleStates.MechanicSlot`), P1's before P2's.
  `context.Owner` is that fighter; act for it only. In a mirror the same instance runs in both slots, each with its
  own slice and config.
- A gauge (`IMeterGauge`) serves its channel for its owner only, so mirrors don't collide; a lineup gauge on the same
  channel fails loading and match start.
- Scenarios read its slice with `p1 mechanic <i>`; use a fixture character whose data names it.

## Switching modules during development

Edit `enabled` or a config value in `data/rules/modules/<name>.json` (or the hook order in `modules.json`), then
restart the local match (debug panel F1 → Save, which reloads data). The new match has the new `MatchRules` and
rules hash. Exported builds read embedded data, so their rules are fixed.

## Removing a module

Delete its folder, its registration line, its data file and its `order` entry; then build and test. For a character
mechanic, remove the `mechanic` object from every character naming it instead of a data file.
