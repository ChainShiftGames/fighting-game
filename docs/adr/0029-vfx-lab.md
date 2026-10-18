# ADR-0029: VFX Lab: a dev-only Godot scene to design effects in rounds of variants

- **Status:** Proposed (Oct 2026): awaiting both devs
- **Gaps:** ROADMAP GAP-7 (art pipeline)

## Context

Agents write the effect shaders (ADR-0028 §9). The dark fireball's look was picked from browser mockups (WebGL), then
ported to Godot by hand. Porting is slow and the look can drift. The devs want to brief an effect in plain text, see
many really different directions side by side, tweak them freely, and drill down on the ones they like, all in the
same style as the character's existing effects.

## Decision

1. **The VFX Lab is a dev-only scene, `game/VfxLab/LabRoot.tscn`.** Run it from the editor (open it, F6) or with
   `--path game res://VfxLab/LabRoot.tscn`. Export presets exclude `VfxLab/*`; its C# ships in the assembly but nothing
   in the game reaches it. No new dependency.
2. **It draws the real thing.** Variants are Godot shaders on the same camera-facing cards the game uses
   (`ShaderCard`), over the real stage (`StageSet`), framed by the match camera at 1 m = 70 px (ADR-0027). They play on
   logic frames at 60 Hz through the same parts the game has: **charge** (startup), **flight** and **impact**
   (ADR-0028 §9). A picked variant is promoted by copying its shader and values into `Assets/Effects/`; there is
   nothing to port.
3. **A sheet per effect being designed:** `game/VfxLab/Sheets/<sheet_id>/sheet.json` (developer setting,
   presentation, in git), next to its draft shaders. It holds the brief, the character's style (palette and motifs,
   so every variant starts on-model), the move's timing (from its data) and **rounds** of variants: a wide first
   round of directions, then deeper rounds on the picks. A variant names a shader per part and values for its
   uniforms; `extends` takes another variant's parts and values and overrides some, so a round of refinements is a
   few lines. An invalid sheet is refused with the reason, never fixed up.
4. **Controls come from the shaders.** Every uniform a variant's shaders declare becomes a control: float → slider
   (range from `hint_range`, else from the sheet's `ranges`, else 0 to twice the default), int → whole-number slider,
   `source_color` → colour picker, bool → checkbox, vec2/vec3 → one slider per component, grouped by
   `group_uniforms`. The uniforms the Lab drives itself (the clock `t`, `spin`, `size`, `progress`, `age`, `kind`,
   `lifetime`, `spawn_size`, `card_size`, `card_centre`) never get a control. Adding a knob is adding a uniform.
5. **Driver uniforms match the game's art nodes:** `t` = seconds from the part's first frame, `spin` = frame × turn
   per frame; flight `size` grows from a start scale over grow frames (eased out); charge `progress` 0 → 1 over the
   startup and `spawn_size`; impact `age` in frames, `kind` (`ImpactKind`) and `lifetime`. Turn speeds, grow and card
   sizes are per-part settings with their own sliders.
6. **Feedback is a file the agent reads:** `feedback.json` next to the sheet, written by the Lab: per variant a
   pick, a 1–5 rating, a hidden flag, a note and every changed value; plus **forks** (a tweaked copy saved as a new
   variant, so the devs can drill down themselves). The agent folds picks and forks into the next round. "Copy
   settings" puts one variant's values on the clipboard for the chat.
7. **It reloads live.** Every half second the Lab checks the sheet, its shaders and `Assets/Effects/` for changes and
   reloads them, keeping playback and tweaks, so an agent's edit shows up without a restart.
8. **Agents can check their own work:** `-- --lab-capture <png> [--lab-frame <n>]` renders the sheet's grid at one
   frame, saves it and quits (needs a GPU, so not in CI).

## Consequences

- New effects start in the Lab: brief → style → wide round → picks and forks → deeper rounds → promote.
- The dark fireball's sheet is the first content; its flight shaders gain `hint_range` and `group_uniforms` so the
  Lab shows sensible sliders. No visual change in the game.
- Not decided: reading timing straight from the move's data file (the sheet copies it for now), sound in the Lab, and
  effects that are not projectiles (hit sparks, supers, traps that aren't projectiles): add parts when the first one
  needs it.
