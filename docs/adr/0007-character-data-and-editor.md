# ADR-0007: Character data format and editor

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-10, GAP-25, GAP-26, GAP-27

## Decision

1. Character data is **JSON**: one file per character and one per move, each with a schema version. Humans,
   AI agents and the editor all edit the same files. At load time they're converted to compact integer data
   for the game logic.
2. The **editor is a Godot editor plugin** that can change every aspect of a character: moves, frame data,
   hit/hurt/push/throw boxes, cancels and all mechanics. Frame-by-frame preview with boxes; changes reload live
   into a running training match.
3. The logic reports each fighter's **frame state** (startup/active/recovery/stun/invincible) every frame, so
   training mode can show a frame meter.
4. **Savestates** in training mode reuse the rollback snapshots. They are kept in memory for as long as training
   mode is open (no time limit) and are not saved to disk, so they never need versioning.

## Godot compatibility

JSON works fine with Godot: the C# code reads it with `System.Text.Json`, both in the game and in the editor
plugin. One gotcha: Godot doesn't treat `.json` as a resource it imports, so the export presets must include
the data files (e.g. include filter `data/**/*.json`), otherwise exported builds ship without characters.
A CI check on the exported build will catch this. *Amended by ADR-0015: `data/` is outside `res://`, so exports
embed the data in `Sim.Data.dll` instead.*
