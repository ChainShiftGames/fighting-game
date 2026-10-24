# ADR index

One line per decision. Read the full ADR only when it's relevant to your task.

| ADR | Covers |
|---|---|
| [0001](0001-pure-csharp-simulation.md) | Game logic is engine-free C#; dependency direction; pinned Godot/.NET versions |
| [0002](0002-input-defaults.md) | SOCD fixed rule in the logic (L+R neutral, U+D up), 2 f button buffer, 12 f motion window (developer settings) |
| [0003](0003-determinism-and-sim-performance.md) | Integers only, banned APIs + custom analyzer (FG0001–FG0004), 8-frame rollback, ≤ 1 ms tick, no allocations |
| [0004](0004-game-loop-and-camera.md) | Own 60 Hz loop, input read timing, vsync, latency counter, camera as gameplay |
| [0005](0005-online-architecture.md) | Steamworks.NET transport (route: ADR-0014), match drivers, rollback effects, delay, desyncs, handshake, replays, match rules locked for players |
| [0006](0006-testing-and-ci.md) | Minimal CI, plain-text scenario tests, golden replays, on-demand soak test |
| [0007](0007-character-data-and-editor.md) | JSON character data, Godot editor plugin, frame meter data, in-memory savestates |
| [0008](0008-presentation-direction.md) | Placeholders first, visual toggles, target look, Godot audio, accessibility incl. directional audio |
| [0009](0009-architecture-tests.md) | ArchUnitNET tests enforcing dependency and module rules |
| [0010](0010-attack-button-layout.md) | A B C D + Special + Dash, button set in data, 16-bit input word with spare bits |
| [0011](0011-module-contract.md) | Module contract, reserved core capabilities (time scale, freeze, cancel, hitstop, entities, gravity, stun, module events, cancel into neutral, move lock, meter gauge), air guard, cancel window and hit adjustment questions, `MatchRules` struct + hash test, module lineup + toggles in data |
| [0012](0012-dev-and-test-dependencies.md) | xUnit v3 on Microsoft.Testing.Platform; Godot AI editor addon vendored dev-only, excluded from exports, telemetry off |
| [0013](0013-spike-a-backdash-go.md) | Spike A: Backdash stays (no fork) on 0.7.8-preview through Phase 1; measurements, problems found, follow-ups |
| [0014](0014-spike-c-steam-p2p-go.md) | Spike C: Steam Networking Messages stays; Steam picks direct (friends) or relay; measurements, problems, follow-ups |
| [0015](0015-shipping-game-data.md) | Exports read `data/**/*.json` embedded in `Sim.Data.dll`; editor and tests read the `data/` folder |
| [0016](0016-game-tests.md) | `tests/FightingGame.Tests` may reference `game/` for engine-free tests; only `game/` and its tests use Godot |
| [0017](0017-core-throws-counter-hits.md) | Throws (with tech) and counter hits are core, always present; air block is a removable module |
| [0018](0018-replay-format.md) | Replay file layout, same-build-only playback, full `MatchRules` in the header, recording around the game logic |
| [0019](0019-desync-checksum-link.md) | Desync checksums ride the match transport as a second message kind; unreliable, repeated until acknowledged |
| [0020](0020-special-button-shortcuts.md) | Special button shortcuts: motion set maps motions to a direction (236 → 5, 623 → 6, 214 → 4), direction + special + the move's button; real motions win; damage and cooldown trade-off in `MatchRules` (off) |
| [0021](0021-data-migration-and-export.md) | Game reads only current schema versions; format changes ship a `tools/DataMigrate` step; external tools read a `tools/DataExport` file |
| [0022](0022-multi-hit-moves-and-juggles.md) | Moves list hits with own frames (each connects once); launch, air launch, ground/wall bounce, knockdown length, OTG per hit; air recovery a match rule |
| [0023](0023-character-mechanics-resources-move-conditions.md) | *Proposed* (built, awaiting both devs): character-scoped mechanic modules, move roles, resource channels, fighter flags, move conditions, core follow-up cancels |
| [0027](0027-3d-match-view.md) | *Proposed*: match drawn as a 3D scene on the 2D gameplay plane (1 m = 70 px), camera placed from the logic camera (orthographic, square-on), 2D layers on top, `IFighterArt` swap point |
| [0028](0028-fighter-models-and-contact-frames.md) | *Proposed*: `RiggedFighterModel` seeks clips named after move IDs to the logic frame; per-character art file in `game/Assets/Characters/<id>/` picks placeholder or model and holds contact frames and timing keys (not in data, not hashed); per-phase retiming; editor animation track |
| [0029](0029-vfx-lab.md) | *Proposed*: dev-only VFX Lab scene: sheets of effect variants in rounds, controls generated from shader uniforms, feedback file and forks, live reload |
| [0030](0030-stage-art-pipeline.md) | *Proposed*: stages use the perspective camera; depth layers (Front, Floor, Middle, Back, Sky, Effects, Lighting); AI key art at the game's framing, split by depth and projected; fighters own the value extremes; per-stage folder copied from `game/Assets/Stages/_template/` |
| [0031](0031-hit-sparks.md) | *Proposed*: hit events carry the hit index; spark looks derived from hit data (weight from hitstop, angle from launch, tear for specials/heavies), per-hit `angle`/`offset`/`style`/`scale` overrides in the art file; one seeded shader, no Godot particles |
| [0032](0032-control-panel-and-effect-settings.md) | *Proposed*: developer control panel as an editor plugin (mechanics, match rules, character stats, hit feel table, effects); saves only what the game's readers accept; effect feel in `game/Assets/Effects/effect_settings.json`, outside data/ |
| [0033](0033-input-consumption.md) | *Proposed*: a press that starts an action (move, cancel, dash, air action, accepted module cancel) is used up; global freezes no longer age input |
| [0034](0034-mixamo-prototype-character.md) | *Proposed*: Mixamo's Kachujin and Mixamo fighting clips as the prototype fighter; free for games, raw files never redistributed; clips packed in place into `<id>_moves.glb` by `tools/Blender/mixamo_clips_to_glb.py` |
| [0035](0035-illustrated-brush-toon-look.md) | *Proposed*: fighters aim at an E's Laf++-style illustration: painted shade, dry-brush shade borders, light patches, wobbly thin ink outlines, inner ink lines pass; toggles in the debug panel |
| [0036](0036-golden-replays-and-testman-scenarios-on-hold.md) | *Proposed*: golden replays and testman scenarios skipped while testman's tuning changes often; fixture-character scenarios still run |
| [0037](0037-lingering-projectiles.md) | *Proposed*: projectile data `rehitInterval` (default 0 = gone on first hit): above 0 it stays out after landing and lands again after that many frames (floor fires, traps); entity `RehitCooldown` |
| [0038](0038-device-roster-and-assignment.md) | input device roster (one keyboard + joypads, GUID+ordinal ids), per-player device assignment read each frame, default = P1 keyboard + nth pad to player n, claims new pads; a lost pad holds local matches until replugged or Start on a free pad, online keeps reading |
| [0039](0039-menu-framework.md) | *Proposed*: menu framework: plain C# `MenuStack` of `IMenuScreen`s with focus memory, `MenuAction` flags tagged with the device, held-direction repeat and input grace (`MenuTiming`), item widgets, shared theme (Marcellus + M PLUS 1p fallback), CSV translation keys via `MenuText` |
| [0040](0040-game-flow.md) | *Proposed*: game flow: plain C# `GameFlow` decides the next screen and starts/stops matches only via `IMatchStarter`; screens register once in `FlowScreens`, missing ones are skipped; `MatchSetup` (mode + picks; devices stay in `PlayerDevicesChoice`, stage with UI-06); leaving a match returns to where its mode was picked; item descriptions under every item |
| [0041](0041-character-select.md) | *Proposed*: character select: colours (`#rrggbb` list) and description key in `game/Assets/Characters/<id>/<id>_select.json`, not in data/ or its hash; colour index travels via `FighterPick` into the replay header; plain C# selection rules (mirror colour shift, random, session memory, no-controller side mirrors); `IMenuScreen.TakesEveryPress` and custom views for two-player screens |
| [0042](0042-online-picks-and-lobby-screens.md) | *Proposed*: online picks as Steam lobby member data (`pick_for`, opaque to `LobbyFlow`), lobby stage `Picking` between ready and start, rematch keeps picks, P1 = owner's pick; host's stage once UI-06 exists; lobby ping from Steam ping locations; handshake refusal reasons as flags |

New ADR: copy the format of an existing one, use the next number, add a line here.
