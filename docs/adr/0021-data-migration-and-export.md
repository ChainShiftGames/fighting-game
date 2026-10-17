# ADR-0021: Data format changes go through a migrator; tools read an export

- **Status:** Accepted (Oct 2026): Lukas's call
- **Gaps:** foundation review before Phase 4 (schema versions only match exactly; no external tools)

## Context

`data/` files carry a schema version and the loader accepts only the current one (`SchemaVersion.Check`). Growing
the move format for real characters (multi-hit moves, juggles, character mechanics; ADR-0022, ADR-0023) rewrites
every move file, and the roster will reach thousands of files. Lukas also wants to build tools outside the game to
view and tweak characters.

## Decision

1. **The game reads only the current schema version** of each file kind. Old formats never stay in the loader.
2. **A format change ships with a migration** in `tools/DataMigrate`: one class per step (file kind, version N to
   N + 1) that rewrites the JSON tree. The tool runs every pending step over a data root (`data/`, test fixtures)
   and writes files in the shared hand-written style (`Sim.Data.Json.JsonDataWriter`, also the editor's writer).
   The rewritten files are committed in the same PR as the format change; running the tool again changes nothing.
3. **External tools read an export, not the source files.** `tools/DataExport` writes one file per character with
   its resolved data (every move's frame data, hits, boxes, cancels, projectiles, as the game sees it after
   validation), with its own `exportVersion`. Any tool (web viewer, spreadsheet, Blender script) can read it without
   .NET; .NET tools may reference `Sim.Data` directly instead. Tools that edit write the source JSON, which the
   game validates as always.

## Consequences

- A schema change is: bump the version constant, change the reader and schema, add the migration step, run the
  tool, commit the rewritten data and the fixture hash/golden replay updates.
- The export is a read model: changing it never affects the data hash or the game.
