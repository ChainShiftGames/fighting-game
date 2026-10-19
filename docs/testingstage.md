# Building the first stage: Magic Ruins

Walkthrough of the [ADR-0030](adr/0030-stage-art-pipeline.md) pipeline for stage 1 (P4-D). What goes in which folder
and the "done when" checks: [the template README](../game/Assets/Stages/_template/README.md). Work through the steps
in order; each ends with a checkpoint in the running game.

## Step 1: Copy the template and wire it in

- `game/Assets/Stages/magic_ruins` is already copied from the template; reference images go in its `reference/`.
- `game/Assets/Stages/stage_art.json`: `"scene": "magic_ruins/stage.tscn", "perspective": true`. The castle hall
  stays in the repo; switching back is one line.
- **Checkpoint:** the match runs on the placeholder floor, both fighters on it, the HUD unchanged.

## Step 2: Make the camera guide

- Turn on the `Guides` node, run the match in a 16:9 window (the default size is), and take a screenshot at round
  start with the camera centred. The `stage_projection` shader assumes exactly this frame.
- Paint over it in flat shapes: floor line, the two fighter silhouettes, the walls. Save it as
  `reference/camera_guide.png`.
- Every concept image is generated from this guide, so the horizon, floor height and fighter scale match the game.

## Step 3: Generate concepts, pick the key art

- Fill in `reference/notes.md` first: palette, image model, style reference.
- Use the camera guide as the image-to-image or structure input (not a free 16:9 prompt). Starting prompt:

  > Anime fighting game stage, ancient ruin in a magical forest, side-on view. Wide flat stone floor across the
  > lower third with a clear empty fighting area in the centre. Giant trees, broken stone arches, mossy pillars and
  > luminous cyan and violet crystals frame the left and right edges. Tiny magical motes float at different depths.
  > Hand-painted stylized look, clean shapes, soft atmospheric depth, mid-value colours, no characters, no text,
  > no UI.

- Generate several, choose one with strong edge framing, a simple floor, and a background that doesn't compete with
  the fighters. Don't use published games' art as an input (ADR-0030 §4).
- Fix it into key art (paint-over or more passes): `reference/magic_ruins_key_art.png`. Save the prompts in
  `notes.md`.

## Step 4: Split the key art into layers

- Make a depth map of the key art (any depth-estimation tool) and decide the cuts: sky, 2–3 back layers, middle
  props, floor.
- Cut each back layer out with alpha; inpaint what the layer in front of it hid. Working files in `source/`.
- Export the cards to `sky/` and `back/` (`back_ruins_far.webp`, `back_trees_mid.webp`, …).
- **Checkpoint:** the cards stand as `QuadMesh`es with `stage_card` under `Sky` and `Back`; with the camera at each wall nothing
  shows its edge, and the layers slide past each other as the camera moves.

## Step 5: Block out the floor and middle

- With the key art overlaid on the camera view, block out the floor and the big middle shapes (arches, pillars,
  crystal clusters) from `BoxMesh`/`CylinderMesh` until they line up with the painting.
- Floor top at y = 0; nothing in `Front` over the play space.
- **Checkpoint:** in grey boxes, the composition already looks like the key art.

## Step 6: Texture the floor and props

- Floor: stylized tileable textures matching the palette on `stage_toon` (`uv_scale` repeats them), in `floor/`.
- Middle: `stage_projection` with the key art on the block-out, or `stage_toon` with flat palette colours.
  Widened the key art sideways? Only the file changes; widened it upward too, set `art_bottom`/`art_height`.
- Replace a block-out piece with a real model (`.glb` in `middle/`) only where the camera's travel shows the
  projection stretching. AI-generated models are allowed here after cleanup in Blender (origin at the base, real
  scale, no stray geometry, textures embedded); never for the floor.
- Thinner outlines than the fighters, or none.

## Step 7: Add motion

- Under `Effects`: a `GPUParticles3D` of motes (start around 150–250, lifetime 5–10 s, an emission box covering the
  visible arena, slow drift), small emissive quads in cyan, violet and pale gold.
- Cheap life on `stage_card`: `sway_amount` on foliage, `scroll_speed` on mist, `flicker_amount` on crystal glow.
- Motes stay subtle: they must never hide attacks, hit sparks or silhouettes.
- Particle amount and tint are tuned in the scene for now; a per-stage config only once a second stage needs it.

## Step 8: Light and grade

- `Lighting`: the key light, ambient, and fog that fades `Back` into the sky colour. Save the environment in
  `lighting/`.
- Glow only on crystals and motes, restrained.
- Judge it with both fighters in the match, never the stage alone; the fighters keep the darkest and brightest
  values (ADR-0030 §5).

## Step 9: Check it in the match

- [ ] Floor line matches the logic floor; fighters stand on it at both walls.
- [ ] Camera framing and HUD unchanged.
- [ ] Template README "Done when" checks pass (grayscale, blur, Steam Deck resolution).
- [ ] Frame time on target hardware close to the castle hall's; measure before adding particles or lights.
- [ ] Guides off, `.gitkeep` removed from filled folders.

## Later

- What worked here goes back into `_template` (materials, particle setup, lighting rig), so the next stage starts
  from it.
- Per-region, season and weather variation (mote density, tint, rain, mist) is a later design: prove one stage first.
