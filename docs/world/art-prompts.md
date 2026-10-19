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
