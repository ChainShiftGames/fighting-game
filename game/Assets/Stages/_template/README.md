# Stage template

Copy this folder to start a stage. Why it's laid out this way: [ADR-0030](../../../../docs/adr/0030-stage-art-pipeline.md).
Step-by-step walkthrough (first stage): [docs/testingstage.md](../../../../docs/testingstage.md).

## New stage

1. In Godot's FileSystem dock, right-click `_template` → **Duplicate** and name it `<stage_id>` (lowercase
   `snake_case`, e.g. `magic_ruins`). Duplicating in Godot gives the copy its own resource IDs; a plain file copy
   doesn't.
2. Delete `.gitkeep` from each folder once it holds a real file.
3. Point `game/Assets/Stages/stage_art.json` at it:
   `"scene": "<stage_id>/stage.tscn", "perspective": true` (and no `background`).
4. Run the game: the placeholder floor and grey sky show; the stage is wired.

## What goes where

| Folder | Scene node | Depth (z, m) | Holds |
|---|---|---|---|
| `reference/` | — | — | Concept images, key art, camera guide screenshot, `notes.md` (prompts, palette). Not imported |
| `source/` | — | — | Layered paintings, depth maps, inpaint masks, `.blend` files. Not imported |
| `sky/` | `Sky` | about −150 | Sky gradient and card |
| `back/` | `Back` | −20 to −120 | Painted cards cut from the key art (`.png`/`.webp` with alpha), unshaded |
| `middle/` | `Middle` | −3 to −20 | Prop meshes (`.glb`), their textures and materials |
| `floor/` | `Floor` | +3 to −3 | Floor mesh, textures and material; top surface at y = 0 |
| `front/` | `Front` | 0 to +3 | Foreground silhouettes, bottom corners only |
| `effects/` | `Effects` | any | Particle materials, flipbooks, sway/scroll shaders |
| `lighting/` | `Lighting` | — | Saved `Environment`, colour grade resources |
| (root) | `Stage` | — | `stage.tscn` only |

- Depths are starting values; tune them in the real camera view.
- **Guides** (hidden node): logic walls and two fighter-sized boxes at the round start. Turn them on while building,
  off before saving.
- Behind the plane, the camera sees wider than the walls (x = ±8.57 m): the further back a layer stands, the further
  sideways it must reach. Check with the camera pushed against each wall.
- File names: `snake_case`, subject first (`ruins_arch_left.glb`, `back_cliffs_far.webp`).
- Stage code, if any: `game/Presentation/World/Stages/<StageName>/`, not here.

## Done when

- Fighters and hit sparks read clearly in a grayscale and a blurred screenshot at 1280×800 (Steam Deck).
- Nothing in `Front` covers the space the fighters play in, at either wall.
- The stage's darkest and brightest values are less extreme than the fighters'.
