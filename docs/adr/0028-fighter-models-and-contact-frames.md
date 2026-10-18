# ADR-0028: Fighter models: a glTF per character, picked by an art file that also holds contact frames

- **Status:** Proposed (Oct 2026): the skeletal `IFighterArt` from ADR-0027 §6; awaiting both devs
- **Gaps:** ROADMAP GAP-7 (art pipeline)

## Context

ADR-0027 made `IFighterArt` the swap point for fighter art. The asset spec is agreed: glTF 2.0 `.glb`, humanoid base
rig (Godot humanoid bone map) plus extra bones, faces +X, Y up, metres, in place (no root motion), 60 fps, simple
materials; clips named after move IDs; hitboxes stay data. Frame data gets tuned far more often than animations get
re-exported, so a clip must follow the move's startup/active/recovery without re-authoring, and the hit must land on
the authored contact pose. Something has to say where that pose is, and which characters use a model at all.

## Decision

1. **`GltfFighterModel` is the second `IFighterArt`.** It instances the character's imported `.glb`, binds one clip
   per move ID, and each displayed frame seeks the `AnimationPlayer` (process mode Manual, paused, method/audio
   tracks off) to the time for the logic's move frame. It never advances by elapsed time. The model root stands on
   the logic position and is mirrored (scale X = facing), not turned, for facing.
2. **Per-character art file: `game/Assets/Characters/<id>/<id>_art.json`** (developer setting, presentation, in
   git), read by `FighterArtFile`, next to the `.glb` it names:
   ```json
   {
     "artVersion": 1,
     "art": "model",
     "model": "testman.glb",
     "scale": 1.0,
     "turnDegrees": 0,
     "animations": { "5a": { "clip": "Punch", "activeFrom": 6, "recoveryFrom": 10 } }
   }
   ```
   - `art`: `placeholder` or `model`. **No file → placeholder.** This is the switch; no code names a character.
   - `animations` is optional per move: `clip` overrides the move-ID name (for third-party test models);
     `activeFrom`/`recoveryFrom` are the contact frames. `scale`/`turnDegrees` only adapt off-spec models.
   - An invalid file is refused with the reason (error in the log) and the character drawn as a placeholder.
3. **Contact frames live in the art file, not in character data.** They are animation facts: they change when an
   animator re-exports, never when the game rules change. In `data/` they would enter the data hash (an art tweak
   would block online matches between otherwise identical builds), need a schema bump and `tools/DataMigrate` step
   (ADR-0021), and put presentation data into `Sim.Data`. No schema change.
4. **Retiming (`AnimationRetiming`, engine-free and tested):** clip frames at 60 fps; move frame 1 shows clip time 0.
   - No contact frames, or a move without active frames (idle, walk, stun): frame for frame, time = (frame − 1)/60.
   - With contact frames, three linear spans: startup frames → clip `0..activeFrom`, active frames →
     `activeFrom..recoveryFrom`, recovery frames → `recoveryFrom..end`. The first active frame always shows
     `activeFrom`. Multi-hit moves (ADR-0022) use first-hit start to last-hit end as "active" for now.
   - Past the end: looping clips wrap, others hold the last pose. Hitstop holds the pose, as the move frame holds.
5. **Missing clip → idle, one warning per move; missing idle → rest pose.** A contact frame past the clip end is an
   error in the log and the clip plays frame for frame. Nothing here crashes a match.

## Trying it (no model in git yet)

Third-party models stay out of git until the art direction picks one. Quaternius' *Universal Animation Library*
(Standard tier, free, CC0: commercial use allowed) has a humanoid `.glb` with clips such as idle, walk and punches:
download it from quaternius.itch.io/universal-animation-library, copy the character `.glb` to
`game/Assets/Characters/testman/testman.glb`, open the editor once so it imports, and add `testman_art.json` with
`"art": "model"`, `"turnDegrees": 90` (glTF models usually face +Z) and `clip` names for the moves to try. Moves
without a clip show idle.

## Consequences

- Frame data can change without touching animations; animators change contact frames without touching game data.
- One `.glb` per character; clips that do not match a move ID are ignored. Asymmetric characters look mirrored
  when facing left (as in most 2D fighters); a per-side model is a later decision.
- Not decided: per-hit contact frames for multi-hit moves, blending between clips (none now: poses snap, as the
  logic does), and export of the art file (JSON in `res://` is packed with `all_resources`; check on the first
  export).
