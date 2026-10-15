# Fighting Game — Roadmap & Open Questions

> Companion to [CURRENT_STATE.md](CURRENT_STATE.md). Decisions are recorded as ADRs in [docs/adr](docs/adr).

- Still to review later: Ikemen GO / MUGEN character format, Snopek's Godot rollback addon (frame-timing pitfalls),
  GDC "8 Frames in 16ms" (NetherRealm rollback), GDC 2015 Guilty Gear Xrd art talk, Infil's netcode article, Dustloop wiki.

**How to read this file.** Each phase lists its scope, its exit criteria, the technical decisions already made for it,
and what's still open. `(GAP-n)` tags trace back to the Oct 2026 gap review. **Open** = needs discussion before or
during the phase; **Later** = consciously postponed and must not block current work.

**Overall direction:** systems first, visuals later. Development uses placeholder art until most systems are built.
Visual decisions never block gameplay work. DLC is far off and must not shape current design.

---

## 7. Phased roadmap

Each phase ends with exit criteria verified by humans playing, not just green CI.

### Phase 0 — Foundation & spikes

- Repo structure, CI (GitHub Actions), CLAUDE.md files, ADRs, spec template, session-start hook.
- `ARCHITECTURE.md` describing how the code is split and how one frame flows.
- **Spike A (netcode):** Godot .NET + Backdash + toy sim (two squares); SyncTest in CI; two instances over UDP with simulated lag.
- **Spike C (Steam):** Steamworks.NET adapter over Steam's relay using test AppID 480 (no fee needed yet).
- *Spike B (art) moved to Phase 4.*
- **Exit:** ADR per spike; go/no-go on Backdash; versions pinned; architecture doc agreed.

**Decided**
- **Code structure** ([ADR-0001](docs/adr/0001-pure-csharp-simulation.md), GAP-1): game logic is a pure C# library
  with no Godot dependency. Godot only reads devices, draws and plays sound. Dependencies point one way:
  Godot game → netcode → logic.
- **Versions** ([ADR-0001](docs/adr/0001-pure-csharp-simulation.md), GAP-2): latest stable Godot 4.x .NET and the
  .NET LTS it supports, pinned in `global.json` and the ADR at Spike A. Upgrades are deliberate PRs.
- **Determinism** ([ADR-0003](docs/adr/0003-determinism-and-sim-performance.md), GAP-3): integers only in game logic
  (1 px = 1000 units), seeded PRNG in game state. Floats, `System.Random`, clocks, unordered collection iteration,
  LINQ, `async` and mutable statics are banned in the logic, enforced as build errors by
  `Microsoft.CodeAnalysis.BannedApiAnalyzers`.
- **Speed budget** ([ADR-0003](docs/adr/0003-determinism-and-sim-performance.md), GAP-4, GAP-29): rollback window
  8 frames by default, configurable. One logic frame ≤ 1 ms on Steam Deck, no memory allocation per frame. This is
  the only performance budget; there are no graphics budgets.
- **Game loop** ([ADR-0004](docs/adr/0004-game-loop-and-camera.md), GAP-6): our own fixed 60 Hz loop, input read
  right before each logic frame, vsync on by default with an off toggle, logic stays 60 Hz on high-refresh screens.
  Backdash time sync keeps online players in step.
- **Backdash** (GAP-5): MIT licence, free to use, modify and ship. Kept behind our own match interface so it can be
  forked or replaced.
- **Steam** ([ADR-0005](docs/adr/0005-online-architecture.md), GAP-8): Steamworks.NET (plain C#, MIT) with Steam's
  relay as a custom Backdash transport. GodotSteam rejected because it would tie networking to Godot.
- **CI** ([ADR-0006](docs/adr/0006-testing-and-ci.md), GAP-9): minimal. Every PR: build, logic tests, SyncTest and
  the logic speed benchmark on Linux. Windows build nightly. No UI/visual tests.
- **Repo & files** (GAP-34): free and GitHub-only, no Git LFS for now (placeholder art is small); revisit when real
  art lands. Claude proposes naming conventions in `CONVENTIONS.md`, the devs confirm, Claude follows them.

**Open**
- Contents of `ARCHITECTURE.md` (GAP-0): to be agreed with the devs before writing.

### Phase 1 — Simulation core, placeholder art, offline

- Integer math, entities, input layer (devices, remapping, SOCD) + motion parser, move state machine, base character,
  box collision, hit/block, hitstun/blockstun/hitstop, health, rounds, walls/corners, camera.
- Placeholder character "Testman" (~10 moves), hitbox overlay, scenario tests, 10k-frame SyncTest in CI.
- Built-in input latency counter.
- **Exit:** both devs play local versus on controllers and it already feels responsive; CI green.

**Decided**
- **Character data** ([ADR-0007](docs/adr/0007-character-data-and-editor.md), GAP-10): JSON, one file per character
  and one per move, each with a schema version; converted to compact integer data at load. Works with Godot via
  `System.Text.Json`; the export presets must include the JSON files (checked in CI).
- **Input defaults** ([ADR-0002](docs/adr/0002-input-defaults.md), GAP-11): SOCD left+right = neutral, up+down = up,
  switchable. Button buffer 2 frames; motion window 12 frames. All are data values, easy to tweak in development,
  and part of the match rules.
- **Camera** ([ADR-0004](docs/adr/0004-game-loop-and-camera.md), GAP-13): horizontal position and max player
  distance are game logic; zoom, shake and flashes are visual only.
- **Input latency** ([ADR-0004](docs/adr/0004-game-loop-and-camera.md), GAP-14): top priority, as low as possible.
  Measured automatically by a built-in counter from input read to frame shown, tracked at every milestone.
- **Tests** ([ADR-0006](docs/adr/0006-testing-and-ci.md), GAP-15): plain-text scenario tests that humans and AI
  agents can write ("P1 does 5A point blank → P2 is hit on frame 5"), plus golden replays whose checksums must not
  change unless intended.
- **AI-written code** (GAP-52): no extra rules beyond normal CI.

**Open / Later**
- **Hit resolution rules** (GAP-12, Later): same-frame trades, P1/P2 symmetry, throw vs strike priority. Kept
  configurable so both options stay possible.
- **Attack button layout** (§8 Q3): blocks Testman's move list and the input format, so it should be decided before
  this phase starts.

### Phase 2 — Online

- Backdash integration behind our match interface; P2P over UDP (dev only), then over Steam (minimal lobby / friend invite).
- Network simulator panel, desync detection + dumps, replays, input delay settings.
- **Exit:** play each other online over Steam from different networks, feels good at ~100 ms; 1-hour bot-vs-bot soak without desync.
- Online comes this early to validate the architecture while the codebase is still small.

**Decided** (all [ADR-0005](docs/adr/0005-online-architecture.md) unless noted)
- **Match interface** (GAP-16): one match-driver interface for local, online, replay, training and test matches.
- **Effects during rollback** (GAP-17): the logic emits events tagged with their frame; presentation plays each once
  and cancels effects whose event disappears after a rollback.
- **Network defaults** (GAP-18): online should feel as close to local as possible. Automatic input delay of 1–2
  frames based on ping (player override 0–4). Warning after 3 s without data; match ends without a result after
  10 s. Test conditions: 100 ms ping, ±20 ms jitter, 2 % packet loss.
- **Desyncs** (GAP-19): per-frame state checksum compared between players; on mismatch both sides save a dump
  (state, inputs, version); a dev tool shows what differs. The two devs exchange dump files manually.
- **Handshake** (GAP-20): game version, game-data hash and match rules exchanged before a match; mismatch shows
  "versions differ — update the game".
- **Replays** (GAP-21): small binary file with a header (version, data hash, rules, characters, colours) and the
  compressed inputs.
- **Soak test** ([ADR-0006](docs/adr/0006-testing-and-ci.md), GAP-22): two bots, 1 hour, simulated lag/loss,
  seeded inputs. Runs on demand via a manual CI trigger, not nightly.
- **Security** (GAP-23): Steam first. All traffic through Steam's relay (hides IPs); direct UDP only for dev testing;
  malformed packets dropped.

### Phase 3 — Systems & the Lab

- Full module system: gatlings, counter/punish counter, proration, meter + supers, throws/tech, pushback guard,
  instant block, burst, Roman Cancel v1 + switchable variants, special button + dash button behaviour.
- Training mode (frame meter, record/playback, savestates), control panel, character editor v1, simple CPU bot.
- **Exit:** Testman has the full system kit; a new move can be created in the editor without code; variants switch between rounds.

**Decided**
- **Modules** ([ADR-0005](docs/adr/0005-online-architecture.md), GAP-24): most mechanics are toggles that don't touch
  the core. Rules are locked during a match; changing them in training restarts the round. Online, both players see
  and accept the rules before the match.
- **Frame meter** ([ADR-0007](docs/adr/0007-character-data-and-editor.md), GAP-25): the logic reports each fighter's
  frame state (startup/active/recovery/stun/invincible) every frame.
- **Savestates** ([ADR-0007](docs/adr/0007-character-data-and-editor.md), GAP-26): reuse rollback snapshots, kept in
  memory for as long as training mode is open (no time limit); not saved to disk.
- **Character editor** ([ADR-0007](docs/adr/0007-character-data-and-editor.md), GAP-27): Godot editor plugin that can
  change every aspect of a character (moves, frame data, hit/hurt/push/throw boxes, cancels, all mechanics), with a
  frame-by-frame preview and live reload into a running training match.
- **CPU bot** (GAP-28): presses buttons like a player and never touches internals; simple state machine; difficulty
  via reaction time; the same bot drives soak tests.

### Phase 4 — Vertical slice (most important milestone)

- **Spike B (art)** first: VRoid model → Blender → Godot toon shader, 5 stepped animations, one palette swap. Tests
  whether Claude + Blender MCP + learning key-pose animation is viable; no-go → decide on outsourcing.
- First real character with final-quality art, final toon shader, stage 1, hit VFX, SFX, HUD.
- Game feel tuning pass: hitstop, screen shake, camera.
- **Exit:** online mirror match that genuinely feels great; external FGC playtest done and both devs say it passes.
- Then: Steamworks partner account, $100 app fee, store art + short trailer → Steam "Coming Soon" page live *(timing proposal)*.

**Decided** ([ADR-0008](docs/adr/0008-presentation-direction.md))
- **Visual toggles** (GAP-30): most visual effects (outlines, shading style, shake, flashes, hit effects) are toggles,
  so the visual direction can be iterated on without code changes.
- **Target look** (GAP-30): fixed light per character, Xrd-style hard shading, constant-thickness outlines, 4× MSAA.
- **Audio** (GAP-31): Godot's built-in audio, FMOD only if we hit limits. Loudness target −16 LUFS.
- **Playtest pass** (GAP-33): a playtest passes when both devs say it passes.

**Later**
- **Spike B criteria** (GAP-7): proposals kept for then: about a day or less per animation, check VRoid Studio's
  commercial-use terms, custom normals via Blender Data Transfer, stepped keys, animation frame chosen from the logic
  frame number.
- **Hit effects** (GAP-32): implementation decided later; must not impede development.

### Phase 5 — Roster & content

- Characters 2–4 (each proves new engine features), 2–3 stages, music, palettes/costumes,
  menus, character select, settings, rebinding UI.
- **Exit:** launch content complete; every character passes the uniqueness check.

**Decided**
- **Settings** (GAP-35): settings and keybinds as versioned JSON in Godot's `user://` folder, synced via Steam Cloud.
- **Languages** (GAP-36): English at launch; all text through Godot translation tables and a Japanese-capable font,
  so adding languages later is cheap.
- **Accessibility** ([ADR-0008](docs/adr/0008-presentation-direction.md), GAP-37): full remapping, shake/flash
  toggles, colour-blind-safe colours, scalable text, input display, and **directional audio** (sounds panned by
  on-screen position, distinct cues) for blind players.
- **Uniqueness check** (GAP-38): distinct game plan, at least one unique mechanic, distinct silhouette and sound, no
  copied moves. Details left to implementation time.

### Phase 6 — Online features & Steam integration

- Lobby UI (public/private), quick match via Steam matchmaking, rematch, replay browser.
- Achievements, cloud saves, Steam Input, Steam Deck requirements (button glyphs, readable text at 1280×800, locked 60 fps).
- CI exports Windows + Linux builds and uploads to a Steam beta branch automatically (SteamPipe / steamcmd).
- **Exit:** closed beta via Steam Playtest.

**Later** (revisit before this phase)
- Quick match with a small player pool: ping/region filtering, fallback (GAP-39).
- Steam Deck: native Linux vs Proton, suspend/resume during online matches, glyphs, on-screen keyboard (GAP-40).
- Release engineering: version scheme, branching, build signing, steamcmd secrets, rolling back a bad build (GAP-41).
- Crash reporting, telemetry and GDPR (GAP-42).

### Phase 7 — Beta, polish, launch

- Open Playtest; balance + netcode fixes from feedback; performance on Steam Deck.
- Steam Next Fest demo — **OPEN** (Next Fest runs Feb/Jun/Oct; registration months ahead).
- Release review (a few days), 1.0 launch.

**Later**
- Hotfix and patch rollout process; keeping online on the same build during a rollout (GAP-43).
- Demo build: separate AppID, stripping unshipped content (GAP-44).

### Phase 8 — Post-launch

- Ranked, spectators, additional (paid) characters, balance patches.
- Replays record their game version; old replays play only on that version or are marked unsupported.

**Decided** (light)
- **DLC** (GAP-47): everyone downloads all character data; buying only unlocks picking a character, so anyone can
  fight against it. Far off; must not affect current development.

**Later**
- Ranked backend vs Steam leaderboards; result spoofing in P2P (GAP-45).
- Spectator architecture (GAP-46).
- How old replays stay playable (GAP-48).

### Cross-cutting

- Art track once most systems are built and Spike B says go; placeholders until then. Art is the main bottleneck.
- Weekly playtests; docs updated with code; ADRs for decisions.

**Later**
- Test strategy overview: what runs on PRs vs on demand (GAP-49).
- Logging that's safe inside the game logic, and a debug overlay (GAP-50).
- Third-party licence audit file (GAP-51).

---

## 8. Open questions summary

1. Signature hook mechanic.
2. Setting, tone, name.
3. Attack button layout. **Needed before Phase 1** (Testman's moves and the input format depend on it).
4. Roman Cancel design + alternatives.
5. Steam Next Fest demo: yes/no.
6. Store page timing (proposal: right after the vertical slice).
7. Final roster size and characters (after core works; proposal: 4 at launch).
8. Art pipeline viability (answered by Spike B, now in Phase 4).
9. Contents of `ARCHITECTURE.md` (GAP-0).
10. Technical questions postponed to later phases: see each phase's **Later** list (GAP-53).
