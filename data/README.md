# Game data

Character, move and rules JSON (ARCHITECTURE.md §7, ADR-0007). Loaded and validated by `src/Sim.Data`.

## Layout

```
data/schema/<kind>.schema.json                    JSON Schema per file kind (editor aid)
data/characters/<id>/character.json               stats, move list, basic states
data/characters/<id>/moves/<move_id>.json         one move: frame data, boxes, cancels, hit
data/input/buttons.json                           button set: button ids and input word bits (ADR-0010)
data/input/motions.json                           motions, their priority, motion window and button buffer (ADR-0002)
```

Exported builds embed these files in `Sim.Data.dll`; `data/schema/` is not shipped (ADR-0015).

Naming: [CONVENTIONS.md](../CONVENTIONS.md). Characters: `characters/testman/` (Testman, the placeholder
character, P1-17); a minimal example is in `tests/Sim.Data.Tests/Fixtures/data/`.

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

## Motions (`input/motions.json`)

Matching rules (`src/Sim/Input/Motions/MotionParser.cs`); every leniency is in the data, none is hidden in code:

- A motion is steps, oldest first; each step lists the directions that count for it (`"123"` = any down).
  Directions are relative to the fighter's current facing (6 = forward).
- Other directions may come between steps, so `6236` completes 623 and `25356` completes 236. Consecutive steps
  must not share a direction (holding it would complete both), so 22 is `"2 5 2"` and the 66 dash `"6 5 6"`.
- All steps lie within the window (`motionWindow`, or the motion's own `window`) counted back from the trigger frame,
  inclusive. The trigger is the button press; for a motion used without a button (dashes) it is the frame its last
  direction was entered. The motion must be complete by the press.
- The trigger counts while it is at most `buttonBuffer` frames old: with 2, a press 2 frames ago counts, 3 doesn't.
- Single directions are motions too, with `"window": 1` (direction on the press frame). `5` accepts any direction,
  so jump normals work with the stick held; anything more specific listed earlier wins.
- **Priority:** list order. When several motions match on one frame, the first wins, but only among motions the
  character has a move for with the same button and stance (no 623C → 6236C gives 236C).
- `motionWindow` and `buttonBuffer` are copied into `MatchRules` (developer settings, compared online).
- `movementInputs`: the motions that dash, backdash and high jump (they must be in the list; `28` is the high
  jump, with its own short window) and the optional `dashButton` (a button id), which backdashes when pressed with
  back. The high jump is checked on the frame a prejump starts; a double jump is a fresh up press in the air.
- Every move's `input.motion` must be in the list; `GameDataLoader` reports the move file otherwise.
- `input.buttons` (instead of `button`) lists buttons pressed together, e.g. a throw's `["a", "b"]`: the press is
  the frame the last of them goes down (the others may already be held).

## Throws (P1-23)

- A move with `throw` boxes throws; it needs a `throw` object (`releaseX`: where the opponent lands, forward of the
  thrower; negative for a back throw) and a `hit` (damage, hitstop and reaction apply when it lands). It can't also
  have hit boxes, and throws are ground moves. Category `throw` marks normal throws: their buttons are the throw
  tech input. Rules: `src/Sim/Throws/` doc comments.
- Give a throw enough recovery to cover the tech window (`throwTechWindowFrames`), or the thrower is free before
  its throw lands.
- Basic states `thrown` (held; no hurtbox, so nothing else hits it) and `throwTech` (its velocity separates the
  fighters).
- Basic state `airBlockstun` (P1-24): used only while the air block module is enabled; a landing ends it.

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
