# Stage template

Copy this folder to start a stage. Why it's laid out this way: [ADR-0030](../../../../docs/adr/0030-stage-art-pipeline.md).
Step-by-step walkthrough (first stage): [docs/testingstage.md](../../../../docs/testingstage.md).

## New stage

1. In Godot's FileSystem dock, right-click `_template` → **Duplicate** and name it `<stage_id>` (lowercase
   `snake_case`, e.g. `royal_plaza`). Duplicating in Godot gives the copy its own resource IDs; a plain file copy
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
- Materials (all in `game/Assets/Shaders/`, fog fades them with distance):
  - **`stage_toon`**: floor and props, lit by the stage's lights in hard bands. Outline: `toon_outline` as next pass,
    thinner than the fighters'.
  - **`stage_projection`**: blocked-out shapes that show the key art (or one layer cut from it), projected from the
    round-start camera. Its defaults fit a painting made over the camera guide; a painting widened evenly sideways
    only needs the new file. Paint the guide in a 16:9 window (the default 1152×648 is).
  - **`stage_card`**: `QuadMesh` cards in `Back` and `Sky`, with optional scroll, sway and flicker.
  - **`stage_projection_shadowed`**: the same projection for a painted floor, darkened where the key light's
    shadow falls, so fighters cast shadows on it.
- **`FighterStageLight`** (node under `Lighting`): tints the fighters' toon light with the stage's sun, sky and a rim,
  so they sit in the stage. Without it the fighters look as in every other stage.
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
