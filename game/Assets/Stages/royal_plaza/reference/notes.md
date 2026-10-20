# Royal Plaza: art notes

## Palette (6–8 colours)

Sampled from `royal_plaza_key_art_v2.png`; fill in when it is fixed.

| Role | Hex |
|---|---|
| Floor | |
| Middle props | |
| Back / distance | |
| Sky | `#B8D6E6` (clear colour) |
| Accent (light, magic, signs) | |

## Image model and style reference

- Model and version: Midjourney (16:9 generation, Editor Erase on the floor, widened in 2–3 rounds, Subtle upscale)
- Style reference images (file names in this folder):

## Prompts

Keep every prompt that produced an image we use, with the output file name. Build them from
`docs/world/art-prompts.md`; note the region and purpose blocks used.

| Output | Prompt | Notes |
|---|---|---|
| `royal_plaza_key_art.png` | | 3616×1312 (2.76:1). Not made from the camera guide |
| `royal_plaza_key_art_v2.png` | Style + Aristocratic Region + Stage + content: "eye-level view from standing head height with the horizon exactly at the middle of the image, a palace plaza in the late afternoon, polished pale blue-grey stone paving filling the lower half with soft reflections of the architecture, a low carved balustrade crossing the whole image just below the horizon, beyond it a grand white archway and a suspended bridge between towers, ivy-covered colonnades framing the far left and right edges, a soft turquoise aurora across the upper sky"; avoid also "people, characters" | Retexture of `camera_guide_clean.png`, `--sref` the first key art, 16:9 then panned sideways only to ~2.75:1 |

## Fit to the round-start camera

The stage uses `royal_plaza_key_art_v2.png`; the first key art stays here as the style reference.
`source/build_layers.py` holds the numbers and prints each card's position; the scene uses them.

- Made from the camera guide (Retexture), so it is the guide's frame: the `stage_projection` defaults, horizon on
  the camera's eye level (row 662), the guide's foot line painted as the floor seam at row ~1154.
- The painted floor is projected onto the 3D floor (`floor/floor_key_art.png`, `stage_projection_shadowed`).
  The balustrade base (row 862) stands on the floor's far edge, which puts `Middle` at z = −10.43.
- Cards: `middle/middle_colonnades.png` (balustrade + colonnades, z −10.43), `back/back_palace.png` (palace,
  bridge, valley; z −44.7), `sky/sky_aurora.png` (z −149.7). The sky mask is found by smoothness and colour.
- Near layers slide furthest: at a wall the floor needs the painting out to ±10.7 m on the gameplay plane (~6.6:1)
  and `Middle` to ±5.4 m (~3.3:1); the key art is 2.76:1. Until it is panned wider (sideways only), the script
  widens both by mirroring the painting's outer edges, which shows at the walls as a mirrored colonnade, sky and
  streaky floor.
- Hidden parts are a smooth placeholder fill, seen at the walls only (e.g. inside the arch). To replace it,
  inpaint `back_palace.png` with `source/mask_middle.png` and `sky_aurora.png` with the inverse of
  `source/mask_sky.png`.

## Fighter shadows

Real shadows: the painted floor uses `stage_projection_shadowed`, which shows the painting as painted and darkens it
toward a cool shadow colour where the key light's shadow map is blocked; no blob. The key light comes from the left
but almost overhead, so the shadows pool under the feet; a low sun throws long shadows that look detached in the side view.
`Lighting/FighterStageLight` tints the fighters' own light with the plaza's sun, sky shade and a sky rim.

## Tool licences

Each tool whose output is committed, and that its licence allows commercial use (ADR-0030 §7).

- Midjourney: paid plans allow commercial use of outputs (check the plan before committing).
