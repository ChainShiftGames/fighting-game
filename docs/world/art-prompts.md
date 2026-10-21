# Art prompt template

One prompt shape for every generated image, so only the content changes between images. Visual rules come from
[setting.md](setting.md) §8; a region block changes only when the bible does (canon rule: generated details are art
direction, not lore).

## How to use

1. Same image model and version for everything; record it in the stage's `reference/notes.md`.
2. Attach the region's style reference image (`--sref`, "style reference", IP-Adapter).
3. Build the prompt: **Style + Region + Purpose + Content**, then **Avoid** as the negative prompt (or appended as
   "no …" where the tool has no negative field).
4. Write only the **Content** line yourself. Save it with the output file name in `reference/notes.md`.
5. No game or artist names in prompts (copyright, ADR-0030 §4); describe the qualities instead.

## Style (always)

```
polished fantasy concept art, clear silhouettes, strong environmental storytelling, readable architectural forms,
hand-painted look, clean shapes, soft atmospheric depth
```

## Region blocks

### Aether-Rich Aristocratic Region

Style reference: the creator's approved reference (setting.md §8.2, primary reference).

```
elegant white and ivory stone architecture, intricate arches, columns, balustrades, carved details, elegant towers,
turquoise and emerald-green roofs, elevated bridges and suspended terraces connected to the landscape, gardens and
greenery woven into the buildings, rich blue-and-green plains, rolling green terrain, lush vegetation, aurora-like
turquoise, blue and green sky, luminous Aether formations integrated into the environment and architecture,
refined, prosperous, distinctly magical
```

Region-specific avoid: `entire city floating in the sky, floating islands everywhere, dark gothic, sinister mood,
militaristic architecture, magical effects filling every space`

### The Glass Expanse

Not written yet: only the name is confirmed (setting.md §3). Add a block once its visual reference is approved.

## Purpose blocks (pick one)

### Stage

Use the camera guide (see [testingstage.md](../testingstage.md) step 2) as the image-to-image or structure input.

```
fighting game stage background, side-on view, wide flat floor across the lower third with a clear empty fighting
area in the centre, strong framing elements at the left and right edges, mid-range values so characters stand out,
16:9, no characters
```

### Character

```
full-body character concept, neutral standing pose, plain light background, readable silhouette, clear costume
details, front view
```

### 3D model input (Tripo)

For images fed to Tripo (see [Character to 3D](#character-to-3d-midjourney--tripo)). Replaces the **Style** block:
painted lighting ends up baked into the model's texture and fights the toon shader.

```
character model sheet, full body, A-pose, arms 45 degrees down and away from the body, fingers spread, feet
shoulder-width apart, front view, neutral expression, eyes open, face fully visible, flat even lighting, solid flat
colour regions, clean cel-shaded anime style, sharp clean edges, plain white background
```

Negatives go only in the Avoid list: image models read "no shadows" in the prompt as "shadows".

### Promo / key visual

```
key visual, dramatic composition, cinematic lighting
```

## Content (write per image)

What is in this image: the location or subject, time of day, weather, Aether density, focal point. One or two
sentences. Example for a stage:

```
a royal garden terrace on the edge of the capital at dusk, white colonnades on both sides, a suspended bridge in the
middle distance, a tall turquoise Aether formation rising behind the towers, light mist over the plains below
```

## Avoid (always, plus the region's)

```
text, letters, watermark, signature, logo, UI, frame, border, blurry, low detail, photo, 3D render look,
generic glowing particles everywhere
```

3D model input adds: `dramatic lighting, rim light, cast shadows, highlights, specular shine, ambient occlusion,
painted shading, gradients, hair over face, glasses, hands touching body, crossed arms, held weapon, cropped feet,
motion blur`

## Character to 3D (Midjourney → Tripo)

Natural-language description → Midjourney model sheet → Tripo image-to-3D → rig →
[`tools/Blender/tripo_to_glb.py`](../../tools/Blender/tripo_to_glb.py).

**Agents, asked for a character:** write only the **Content** line from the description (rules below), assemble
the Midjourney prompt, and give the Tripo texture prompt with it. Ask nothing unless the description leaves the
costume's main colours open.

### Content line rules

- Order: who (role, build, age range) → hair → face → top → bottom → shoes → accessories.
- Name 3–5 main colours as flat colours ("crimson", "charcoal black"), each tied to a part. They become the
  palette-swap regions.
- Hair off the face; ponytails, braids and long sashes are fine but each needs its own bones, so keep it to a few.
- Hands empty and open; weapons and props are separate models.
- No thin floating or loose parts (chains, straps hanging free, separate earrings): they come out as broken geometry.
- Slightly large head and eyes, about 6.5–7 heads tall: the face is about 30 px in game.
- Region fashion (setting.md §8.2) only when the character is from that region.

### Midjourney prompt

```
<3D model input block>, <Content line> --ar 2:3 --style raw --stylize 50 --seed <n> --sref <style image URL> --no <Avoid + 3D model input avoid>
```

- `--sref`: one approved character image shared by every character, so the cast matches. `--seed`: keep it per
  character; changing only the Content line then changes only the character.
- Pin the version (rule 1 above). `--oref` (same character in a new view) exists on V7 only; on later versions
  use the chosen front image as an image prompt plus the same seed.
- Optional side and back views for Tripo's multiview input: same prompt with "side view" / "back view" in place of
  "front view". Use them only if they match the front; a mismatched view is worse than none.

### Tripo

| Setting | Value |
|---|---|
| Input | Image to 3D (front), or multiview when side and back match |
| Mesh | Smart Mesh / quad, face limit ≈ 15k quads (≈ 30k triangles) |
| Texture | Detailed (HD); try PBR on and off once per new style and keep the flatter one |
| Seed | Record it with the prompt |

Texture prompt (the texture prompt field, or appended to the main prompt where there is none):

```
flat cel-shaded base colours, unlit, no baked lighting, no shadows, no highlights, no ambient occlusion, clean solid
colour regions, <the Content line's colours>
```

- Faces stay the weakest part: plan to repaint eyes, brows and mouth flat on the texture in Blender.
- Save the Midjourney prompt, seed, Tripo settings and texture prompt with the character's source files.
