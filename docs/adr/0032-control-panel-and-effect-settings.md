# ADR-0032: Developer control panel and the effect settings file

- **Status:** Proposed (Oct 2026): awaiting both devs

## Context

P3-34 asks for a place to toggle mechanics and tune the game. The in-game F1 debug panel edits a flat list of
top-level fields and can't reach character stats, per-hit data or presentation values. Screen shake, spark weights
and rumble were constants in code, so tuning them meant a rebuild.

## Decision

1. **A Godot editor plugin, `game/addons/control_panel/`**, a main-screen tab ("Control Panel") next to the Character
   tab. Pages: Mechanics (every module's `enabled` and config), Match (match rules, input windows, rollback window),
   Characters (stats, with a jump readout computed as `FighterMovement` flies it), Hit feel (every hit of a character's
   moves in one table with frame advantage, and a "scale / add to a whole column" bar), Effects. All
   **developer settings**.
2. **Edits stay in memory until saved; a save first passes the game's own readers**: all of `data/` through
   `GameDataLoader` with the edits laid over it (`OverlayDataSource`, moved to `game/Debug/`), the effect file through
   `EffectSettingsFile`. A file changed on disk since the panel read it is never overwritten. Values are rewritten in
   place, so diffs show only the value. "Apply live" saves each change once it settles; editor runs of the game
   restart the local or training match on the change (P3-43 watch, now also covering the effect file).
3. **Effect feel lives in `game/Assets/Effects/effect_settings.json`**, not `data/`: presentation only, so no data
   hash or schema change (same reasoning as ADR-0028 §3). Sections `screenShake`, `hitSparks` (weight mapping and
   one size multiplier), `rumble`. One field table, `EffectSettingFields`, drives both the reader's range checks and
   the panel. Read at every match start; a missing or invalid file stops the match with the reason, like invalid
   data. The spark's look stays in the shader and the VFX Lab (ADR-0031 §6).

## Consequences

- A new mechanic module appears on the Mechanics page by itself. A new effect value is one field in
  `EffectSettingFields` plus its use.
- The panel reuses the Character tab's look (`MapTheme`, `MapPalette`, `GradientButton`, `UnderlineTabs`,
  `SectionTitle`) and `MoveFacts`, so the two tabs stay one tool; restyling the Character tab restyles both.
- The Character tab keeps its own open copy of a character: save one tab before editing the same files in the other,
  or use Reload.
- Not yet: projectile hits in the Hit feel table, presets/snapshots of whole rule sets, live tuning without a match
  restart.
