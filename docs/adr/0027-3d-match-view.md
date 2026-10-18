# ADR-0027: The match is drawn as a 3D scene on a 2D gameplay plane

- **Status:** Proposed (Oct 2026): Lukas asked for 3D models in a 2D game (as Guilty Gear Strive); awaiting both devs
- **Gaps:** ROADMAP GAP-7 (art pipeline), GAP-31 (rendering)

## Context

The match view draws everything with 2D canvas calls. Phase 4 needs rigged, animated 3D models with toon shading
(ADR-0008 §3), and Spike B is choosing the tools now. The way models are shown in the match does not depend on
those tools, so it is decided first; placeholder art moves onto the same path final art will use
(ARCHITECTURE.md §9). The game logic stays 2D and does not change.

## Decision

1. **2.5D: the stage and fighters are a 3D scene; the gameplay is the plane z = 0.** Logic x/y map to world x/y,
   facing right is +X, y up. The logic never sees the 3D scene; boxes stay data, never derived from bones or meshes.
2. **World scale: 1 Godot unit = 1 m = 70 world pixels (70 000 subpixels)** (developer setting, presentation).
   Testman's 120 px standing hurtbox is then 1.71 m, so models are authored at real-world size.
3. **The logic camera still decides what is shown** (ADR-0004 §5). `StageCamera` keeps fitting the view and adding
   shake; the 3D camera is placed from it (`CameraFraming`) so the plane maps to the screen exactly as the 2D
   layers draw it.
4. **The camera looks straight at the plane, orthographic** (developer setting, a constant for now). A camera that
   faces the plane square-on maps it to the screen with one scale and offset, perspective or not, so a narrow-angle
   perspective camera (Strive-like depth on the stage) is the same framing at a computed distance. Rotated camera
   angles (supers, throws) are a later decision: the 2D layers would then project through the 3D camera.
5. **2D layers stay on top, in the same viewport:** HUD, hit sparks, the hitbox overlay and the placeholder
   readouts (move label, phase strip, active hitboxes) keep their canvas drawing over the 3D scene.
6. **Fighter art is a swap point: `IFighterArt`.** It is given the fighter's pose (`FighterPose`: move and frame),
   position, facing and current boxes each displayed frame. Implementations: the box-shaped placeholder now; the
   skeletal model (`.glb`, animations named after move IDs, in place, picked by logic frame) next.
7. **No new dependency.** Godot's built-in 3D renderer (Forward+ as configured); MSAA and toon shading are the
   Phase 4 rendering task (P4-B).

## Consequences

- Models and animations can be wired in without touching the logic or the 2D overlays.
- Until stage art exists, `game/Assets/Stages/stage_art.json` (developer setting) can stand a picture behind the
  plane, its ground on the logic floor, following a share of the camera's travel (parallax); none: the plain stage.
- The overlay and the 3D scene can drift apart only if the camera is rotated (point 4); that needs its own ADR.
- Next: the skeletal `IFighterArt` ([ADR-0028](0028-fighter-models-and-contact-frames.md)), and
  a renderer check on Steam Deck (Forward+ vs Mobile).
