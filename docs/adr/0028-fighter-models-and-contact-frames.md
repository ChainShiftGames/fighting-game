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

1. **`RiggedFighterModel` is the second `IFighterArt`.** It instances the character's imported `.glb`, gathers one
   clip per move ID into its own `AnimationPlayer` (`MoveClipLibrary`), and each displayed frame seeks it (process
   mode Manual, paused, method/audio tracks off) to the time for the logic's move frame. It never advances by elapsed time. The model root stands on
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
   - `animations` is optional per move: `activeFrom`/`recoveryFrom` are the contact frames; `loop` overrides the
     clip's loop setting.
   - Adapters for off-spec (third-party test) models only: `clip` (a clip name other than the move ID), `file` (the
     clip comes from another imported model file, e.g. one Mixamo FBX per animation), `inPlace` (removes the root
     bone's horizontal travel, keeping height), `scale`, `turnDegrees`. Files are relative to the character folder,
     or a `res://` path. Clips are copied, so imported files are never changed.
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
   - **Timing keys** (`keys`, per move, needs the contact frames, not with `fit`) pin frames inside a span:
     `{ "phase": "startup", "frame": 10, "of": 32, "clip": 20 }` = startup frame 10 of 32 shows clip frame 20, and
     the clip plays straight from pin to pin, so a phase can play its clip unevenly (quick wind-up, linger before the
     hit). `frame` 2..`of` (frame 1 is the span's own start); keys in phase order, then in order within a phase.
     The key sits at `(frame − 1)/of` of its phase, so it stretches with the phase when the frame data changes.
   - Past the end: looping clips wrap, others hold the last pose. Hitstop holds the pose, as the move frame holds.
5. **Transitions and the match clock.** The art file's `transitions` list clips played over the first `frames`
   logic frames of a move entered from certain moves (`from`/`to` lists, optional `reverse`), e.g. one "crouch to
   stand" clip both ways. The game state keeps no previous move, so the model remembers the move it showed last
   and counts frames by the match frame (`FighterPose.MatchFrame`), never elapsed time; after a rollback or a
   rewound replay the match frame goes back and the transition is skipped, a cosmetic loss. Looping clips of basic
   moves count on the same clock, so idle runs through its whole clip and a one-frame crouch still animates.
   Looks only: logic and boxes change on the move's first frame. Hit and block reactions (stun actions) don't use
   the match clock: `HitReactionClock` counts them from each hit by the fighter's stun countdown, so a reaction
   restarts on every hit of a combo and holds in hitstop and freezes (air hitstun: match frames after its hitstop).
6. **Look settings, all by match frames:** a basic move's clip crossfades in from the pose before
   (`crossfadeFrames`, per move or for the file; attacks and hit/block reactions always snap, so an attack's first
   frame is the frame data's and the frame a hit lands shows it);
   a facing switch turns the model through facing the camera, where the mirrored pose looks the same
   (`turnFrames`); `cameraTurnDegrees` (0-90) keeps the model turned that far toward the camera at rest, the 3/4
   view fighting games cheat with, so face and chest read and toon shading falls predictably; `holdFrames` 2 or 3 shows each pose on 2s or 3s (the hand-animated look), per move or for the
   file, held within each frame data phase so the first active frame always shows the contact pose. Per move, `clipFrom`/`clipTo` play part of a clip, `fit` squeezes it into the move (a 4-frame prejump from
   a jump clip's wind-up) and `holdHeight` keeps the root bone's height for a clip that jumps by itself. Poses are
   mixed by an animation tree that never advances on its own.
   `speed` plays a clip faster so a walk cycle's feet keep up with the logic's walk speed. `knockOut` names the
   clip played from the frame a fighter is KO'd (`FighterPose.IsKnockedOut`), whatever its moves do meanwhile.
7. **The character editor shows the same model behind the box canvas** (`ModelPreview`): posed with the same
   retiming from the move's frame data as edited, framed to the canvas's origin and zoom at 1 m = 70 px, so boxes
   are drawn against the body. Placeholder characters keep the body box.
   - **Animation track** (Timing tab, under the frame timeline): the clip frame each move frame shows, with the
     contact frames and keys as pins. Drag a pin, double-click a frame to pin it, right-click a key to remove it, or
     type the clip frame for the picked frame. Edits retime the preview at once and rewrite only that move's
     `animations` entry in the art file on Save (`ArtTimingText`: comments and other entries kept); an edit the
     art file reader would refuse is not made. Keys are rewritten against the phases' lengths at that moment.
8. **Missing clip → idle, one warning per move; missing idle → rest pose.** A contact frame past the clip end is an
   error in the log and the clip plays frame for frame. Nothing here crashes a match.
9. **Projectile art, named per move in the art file:**
   `"projectiles": { "236c": { "flight": "dark_fireball", "charge": "dark_fireball_charge", "impact": "dark_fireball_impact" } }`
   (`ProjectileEffects` lists the names; an unknown one refuses the file). Three swap points: `IProjectileArt`
   (the projectile, by its life frame), `IChargeArt` (the move's startup, at the spawn point, until the spawn frame)
   and `IImpactArt` (hit, block, clash or expiry, from the projectile frame events through the reconciler, so a
   rollback cancels it; it replaces that event's plain hit spark). All run on logic frames, never elapsed time.
   Nodes are made at match start. No entry: the projectile shows only in the hitbox overlay. Effects are Godot
   shaders in `game/Assets/Effects/`, drawn on camera-facing cards that may read the screen behind them (lens and
   shockwave distortion); no new dependency.

## Trying it (no model in git yet)

- Lukas' Mixamo test set (`game/TestModel/`, one FBX per animation) and testman's art file are committed, by Lukas'
  choice (Oct 2026), so every machine runs the same test character. Mixamo's terms allow use in a game but not
  handing out the raw files, and the repo is public: replace them with our own model before release. The art file
  uses `file`, `clip: "mixamo_com"`, `inPlace` and `turnDegrees: 90`.
- Other free option: Quaternius' *Universal Animation Library* (Standard tier, CC0, commercial use allowed;
  quaternius.itch.io/universal-animation-library) has a humanoid `.glb` with idle, walk and punch clips: copy it to
  `game/Assets/Characters/testman/testman.glb`, open the editor once so it imports, and add `testman_art.json` with
  `"art": "model"`, `"turnDegrees": 90` (glTF models usually face +Z) and `clip` names. Moves without a clip show
  idle.

## Consequences

- Frame data can change without touching animations; animators change contact frames without touching game data.
- One `.glb` per character; clips that do not match a move ID are ignored. Asymmetric characters look mirrored
  when facing left (as in most 2D fighters); a per-side model is a later decision.
- Not decided: per-hit contact frames for multi-hit moves, general blending between clips (only listed
  transitions; other moves snap, as the logic does), and export of the art file (JSON in `res://` is packed with `all_resources`; check on the first
  export).
