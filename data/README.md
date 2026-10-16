# Game data

Character, move and rules JSON (ARCHITECTURE.md §7, ADR-0007). Loaded and validated by `src/Sim.Data`.

## Layout

```
data/schema/<kind>.schema.json                    JSON Schema per file kind (editor aid)
data/characters/<id>/character.json               stats, move list, basic states
data/characters/<id>/moves/<move_id>.json         one move: frame data, boxes, cancels, hit
data/input/buttons.json                           button set: button ids and input word bits (ADR-0010)
```

Naming: [CONVENTIONS.md](../CONVENTIONS.md). Example character: `tests/Sim.Data.Tests/Fixtures/data/`.

## Format rules (schema v1)

- Every file starts with `"schemaVersion"`; a file may add `"$schema"` (relative path to its schema) for editor
  validation and autocompletion.
- Strict JSON: no comments, no trailing commas, no duplicate keys, no unknown keys.
- **Integers only.** Distances in subpixels (1 px = 1000), speeds in subpixels per frame, times in frames.
- Frames are 1-based and inclusive. `frameData.startup` counts the first active frame (startup 5 = active from frame
  5); total = startup − 1 + active + recovery. Basic moves use `duration` instead.
- Boxes are given facing right: `x` forward, `y` up from the fighter's origin, as minimum corner + width/height.
- A move's `velocity` ranges (same axes, subpixels per frame) replace the fighter's velocity on their frames and
  suspend gravity; `invulnerable` is one frame range where it can't be hit or thrown.
- `moves` order in `character.json` is input priority. Every listed move file must exist and every move file must be
  listed.
- `data/schema/*.schema.json` documents the format; the validator in `src/Sim.Data` is authoritative and also checks
  cross-file rules. Change both in the same PR.
- All values here are **developer settings** (ARCHITECTURE.md §6); the data hash covers them (compared before online matches).

## Loading pattern (for every new data kind)

Reuse the pieces in `src/Sim.Data`; don't write a second validator.

1. `IDataSource` gives files by data-relative path (`/`, case-sensitive). `DirectoryDataSource` reads a folder.
2. `JsonDataFile.Open` parses a file (syntax errors with line and byte) and returns a `FieldReader`.
3. `SchemaVersion.Check`, then `FieldReader` `Required*`/`Optional*` reads; `Finish()` rejects unknown and
   duplicate keys. Enum spellings go in a `JsonNames<T>` table.
4. Report rule violations with `FieldReader.Error(field, reason)`. Everything lands in one `DataErrors`; loaders
   return null on errors and `DataErrors.ThrowIfAny()` throws one `DataValidationException` listing all of them as
   `file: field: reason`.
5. Convert to compact integer structs (only 4-byte fields; `CompactLayoutTests` checks) and append them to the
   `DataHashBuilder` in `GameDataLoader`, in a fixed order.

## Data hash

SHA-256 over the compact data only (`DataHashBuilder`), never over file bytes or paths, so line endings, BOM,
whitespace, key order and OS can't change it; display names are excluded. `DataHashTests` pins a golden value
for the fixture; CI runs it on Linux, the nightly job and developers on Windows.
