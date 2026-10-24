# ADR-0034: Mixamo character and animations as the prototype fighter

- **Status:** Proposed (Oct 2026)
- **Gaps:** ROADMAP GAP-7 (art pipeline)

## Context

Fleshing out the move system needs a fighter whose clips are actual fighting moves: punches, kicks, blocks, hit
reactions, knockdowns, throws. The CC0 Quaternius libraries (ADR-0028) have few, and the AI-made Tripo models come
with none usable. Adobe's Mixamo has a large fighting library and stock characters on one standard rig, which the
Tripo pipeline already maps to Godot's humanoid profile. Its assets are free but not CC0, so their terms are a
decision (ARCHITECTURE.md §11).

## Decision

1. **Kachujin G Rosales (Mixamo) is the main fighter model for now**, in `game/Assets/Characters/kachujin/`, with her
   Mixamo clips. Moves without a Mixamo clip keep the Quaternius ones; all retarget through the humanoid bone maps.
2. **Licence:** Mixamo characters and animations are free for commercial and non-commercial projects, games included,
   with no royalties (Adobe's Mixamo FAQ). Raw character or animation files may not be redistributed as a product
   (asset packs, engine templates). Our use: converted into the game, in a private repo; never publish the raw or
   converted files on their own (no public repo, asset store or mod kit) without checking the terms again.
3. **Pipeline (developer tools, no runtime dependency):** the character through `tools/Blender/tripo_to_glb.py`
   (Mixamo rig, toon material, bone map); the clips, downloaded "Without Skin" at 60 fps, packed by
   `tools/Blender/mixamo_clips_to_glb.py` into `<id>_moves.glb`, named after the moves, in place (the hips keep
   only their height) and in metres. Which clip serves which move is the art file's job (ADR-0028), a developer
   setting.

## Consequences

- Placeholder art: final characters replace it through the same art file, no code change.
- Adobe has not updated Mixamo in years; if it disappears, downloaded files keep working but no new clips come.
- Re-check the licence before any public release of the repo or a build with these assets.
