# ADR-0035: Illustrated brush look for the fighters (E's Laf++ style)

- **Status:** Proposed (Oct 2026)
- **Gaps:** —

## Context

The fighters' toon shader (P4-B) aimed at Guilty Gear Xrd: one clean lit/shade border, a lilac shade, ink outlines
tinted by the texture. On the current models (detailed, AI-made or Mixamo textures) it still read as a 3D render.
Lukas compared looks in-game (Xrd/Strive variants, flat cel, posterized, ink/comic) and picked one modelled on
E's Laf++ (an indie anime fighter that renders 3D models to illustration-style sprites).

## Decision

1. **The fighters' target look is an illustration, after E's Laf++:** flat colour with little texture detail, a shade
   painted from the texture's own colour, dry-brush (ragged) shade borders, small rough light patches, thin near-black
   ink outlines whose width wobbles, and thin ink lines inside the silhouette. Xrd/Strive stay reference games for
   everything else (§2 Vision).
2. **In the fighters' toon shader** (`toon.gdshaderinc`, `toon_outline.gdshader`), per material: stroke noise is in
   the model's own space, so strokes stick to the model and don't swim; size and strength are uniforms to tune per
   model. Inner lines are a full-screen depth/normal pass on the match camera (`InkLines`), faded out just behind the
   gameplay plane so stages stay painted.
3. **Developer settings** (ARCHITECTURE.md §6, ADR-0008): the look's values are shader uniforms in the material files;
   "Brush shading" and "Inner ink lines" are debug-panel toggles (`LookToggles`), both on.

## Consequences

- Noise strokes are the same everywhere on a model; they don't follow folds as a painter would. A painted per-character
  shade/stroke mask would get closer, if the look stays.
- Stroke size depends on the model's units, so a new model may need its `brush_scale` tuned.
- One extra full-screen pass (a few texture reads per pixel), and it needs the depth and normal-roughness buffers.
