# ADR-0030: Stages are layered sets built from AI key art, seen through the perspective camera

- **Status:** Proposed (Oct 2026): Lukas asked how to build stages that look as good as Xrd/Strive with AI and
  reference images; awaiting both devs
- **Gaps:** ROADMAP GAP-7 (art pipeline), GAP-31 (rendering)

## Context

P4-D needs the first real stage. The castle hall test stage is built in code from shaders and simple meshes; that
doesn't scale to painted, Xrd/Strive-quality stages. Those stages are sets built for one side-on camera: a modelled
floor and near props, painted cards behind, constant small motion, and stage values kept below the fighters'. AI image
tools are good at paintings and textures and weak at style-consistent 3D models, so the pipeline should lean on the
first. [ADR-0027](0027-3d-match-view.md) §4 left the camera projection open: orthographic gives no parallax, so depth
would need per-layer scroll scripts.

## Decision

1. **Stages use the perspective camera** (`"perspective": true` in the stage art file; ADR-0027 §4, field of view in
   `MatchCamera`, developer setting, presentation). Layers then sit at real depths and parallax comes for free; no
   per-layer scroll code. The orthographic camera stays for the plain placeholder stage and the picture background.
   Rotated cameras (supers, throws) remain a later ADR.
2. **A stage is a scene of depth layers**, each a node in the stage scene with its own asset folder:

   | Layer | Depth (z, m) | Built from |
   |---|---|---|
   | `Front` | 0 to +3 | Rare edge silhouettes; never over the play space |
   | `Floor` | +3 to −3 | Modelled floor, the most polished part (always in view) |
   | `Middle` | −3 to −20 | Simple 3D shapes, toon-shaded or with the key art projected on them |
   | `Back` | −20 to −120 | Painted cards (unshaded), cut from the key art, faded by fog |
   | `Sky` | about −150 | Gradient and a painted card, inside the camera's far plane |
   | `Effects` | any | Particles, flipbooks, sway and scroll shaders |
   | `Lighting` | — | Key light, environment, fog, the stage's grade |

   Depths are starting values (developer settings, presentation) to tune in the real camera view.
3. **AI makes paintings, not the stage.** Per stage:
   1. Concept images are generated **at the game's camera framing**: a game screenshot with the floor line and two
      fighter silhouettes guides the generator (image-to-image or a structure guide).
   2. One concept is fixed into **key art** (paint-over or more passes): the target for everything after.
   3. A depth map of the key art splits it into layers; the hidden parts behind each layer are inpainted.
   4. Floor and middle are blocked out in Godot with the key art overlaid on the camera view, then textured by
      projecting the key art from the camera, or with stylized tileable textures on the toon shader.
   5. A prop becomes a real model only where the camera's sideways travel exposes the projection.
   6. Motion pass, then the readability check (point 5).
   AI 3D models (image-to-3D services) are allowed only for `Back`/`Middle` props after cleanup, never the floor.
4. **One style for every stage:** a palette of 6–8 colours per stage, the same image model and style reference across
   stages, prompts saved next to the reference images. Published games' art (Xrd, Strive, …) is studied for its
   qualities only, never fed to a generator for assets we ship (copyright; and it gives a knock-off look).
5. **Fighters own the extremes.** Stage art stays in the mid-values; the darkest darks and brightest highlights belong
   to the fighters. Stage props get no outline or a thinner one than the fighters (ADR-0008 §3). A stage passes when
   both fighters and hit sparks read clearly in a grayscale and a blurred screenshot at Steam Deck resolution.
6. **Folders.** Each stage's art and scene live in `game/Assets/Stages/<stage_id>/`, copied from
   [`game/Assets/Stages/_template/`](../../game/Assets/Stages/_template/README.md) (its README: what goes where;
   [docs/testingstage.md](../testingstage.md): the walkthrough for the first stage). The stage scene is `stage.tscn`, picked by `stage_art.json` (`"scene"`). Code a stage needs goes in
   `game/Presentation/World/Stages/<StageName>/` (namespace = folder, CONVENTIONS.md). `reference/` and `source/`
   carry a `.gdignore`, so Godot neither imports nor exports them.
7. **No new dependency in the game.** Image, depth and inpainting tools are used outside the repo; their licences must
   allow commercial use of the output, checked per tool before its output is committed.

## Consequences

- A stage scene is mostly data an agent can build through the Godot MCP, and the template makes the next stage a copy.
- Layers deeper than the plane show a wider span than the walls (perspective): `Middle` and `Back` must reach further
  sideways the further back they stand.
- The castle hall stays as the code-built test stage until stage 1 replaces it in `stage_art.json`.
- Shared stage shaders in `game/Assets/Shaders/`: `stage_toon` (banded, fogged), `stage_projection` (key art from the
  round-start camera; `StageProjectionTests` fail when the camera settings move away from it), `stage_card`.
- Changing the camera's field of view, visible height or floor line re-frames every stage's key art.
- Next: a decision on Git LFS before large layered or `.blend` source files are committed.
