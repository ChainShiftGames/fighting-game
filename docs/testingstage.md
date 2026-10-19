# Building the first stage: Royal Plaza

Walkthrough of the [ADR-0030](adr/0030-stage-art-pipeline.md) pipeline for stage 1 (P4-D): a palace plaza in the
Aether-Rich Aristocratic Region ([setting.md](world/setting.md) §8.2). What goes in which folder
and the "done when" checks: [the template README](../game/Assets/Stages/_template/README.md). Work through the steps
in order; each ends with a checkpoint in the running game.

## Step 1: Copy the template and wire it in

- `game/Assets/Stages/royal_plaza` is already copied from the template; reference images go in its `reference/`.
- `game/Assets/Stages/stage_art.json`: `"scene": "royal_plaza/stage.tscn", "perspective": true`. The castle hall
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
- Prompt: [art-prompts.md](world/art-prompts.md), Aristocratic Region + Stage blocks. Use the camera guide
  (`camera_guide_clean.png`) as the image-to-image or structure input.
- A concept made without the guide can be fitted afterwards: crop it to the game's horizon and check it with the
  fighters laid over it (`concept_plaza_01_game_frame.png`, `_guide_overlay.png`).
- Key art also needs: nothing standing on or in front of the floor line where fighters play; no foreground foliage
  across the bottom (corners at most); a floor a little darker than near-white; and widened evenly sideways to about
  2.75:1 so the back layers cover the camera's travel to both walls.

- Generate several, choose one with strong edge framing, a simple floor, and a background that doesn't compete with
  the fighters. Don't use published games' art as an input (ADR-0030 §4).
- Fix it into key art (paint-over or more passes): `reference/royal_plaza_key_art.png`. Save the prompts in
  `notes.md`.

## Step 4: Split the key art into layers

- Make a depth map of the key art (any depth-estimation tool) and decide the cuts: sky, 2–3 back layers, middle
  props, floor.
- Cut each back layer out with alpha; inpaint what the layer in front of it hid. Working files in `source/`.
- Export the cards to `sky/` and `back/` (`back_valley_far.webp`, `back_bridge.webp`, …).
- **Checkpoint:** the cards stand as `QuadMesh`es with `stage_card` under `Sky` and `Back`; with the camera at each wall nothing
  shows its edge, and the layers slide past each other as the camera moves.

## Step 5: Block out the floor and middle

- With the key art overlaid on the camera view, block out the floor and the big middle shapes (palace wings,
  towers, the plaza's balustrade) from `BoxMesh`/`CylinderMesh` until they line up with the painting.
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

- Under `Effects`: Aether shown through the scene itself first (aurora, glowing roof gems), per setting.md §8.2;
  then, if needed, a sparse `GPUParticles3D` of turquoise motes near those sources (slow drift, lifetime 5–10 s).
- Cheap life on `stage_card`: `sway_amount` on foliage, `scroll_speed` on mist, `flicker_amount` on the roof gems, a slow
  `scroll_speed` on the aurora card.
- Motes stay subtle: they must never hide attacks, hit sparks or silhouettes.
- Particle amount and tint are tuned in the scene for now; a per-stage config only once a second stage needs it.

## Step 8: Light and grade

- `Lighting`: the key light, ambient, and fog that fades `Back` into the sky colour. Save the environment in
  `lighting/`.
- Glow only on the gems, aurora and motes, restrained.
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
