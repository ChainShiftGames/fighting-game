# Fighting Game — Roadmap & Open Questions

> Companion to [CURRENT_STATE.md](CURRENT_STATE.md).

- Still to review later: Ikemen GO / MUGEN character format, Snopek's Godot rollback addon (frame-timing pitfalls),
  GDC "8 Frames in 16ms" (NetherRealm rollback), GDC 2015 Guilty Gear Xrd art talk, Infil's netcode article, Dustloop wiki.

> **About the `[GAP-n]` markers (review, Oct 2026):** each one marks a technical detail that is missing and needs a
> decision, spec or ADR before we can reach the "robust, AAA-quality" bar. **Needed by** is the latest phase in which
> it has to be settled. [§9](#9-technical-gap-index) lists them all by priority. Remove a marker once its ADR/spec exists.

> **[GAP-0] Missing architecture section.** CURRENT_STATE.md stops after §3 (Meter), and this file starts at §7, so
> §4–6 don't exist. Nothing anywhere describes the architecture that every phase below depends on: the boundary
> between sim and presentation, solution/project layout, the determinism contract, the data formats, the networking
> layers or the tech stack versions. **Needed by: Phase 0.**

---

## 7. Phased roadmap

Each phase ends with exit criteria verified by humans playing, not just green CI.

### Phase 0 — Foundation & spikes

- Repo structure, CI (GitHub Actions: build, test, Godot headless smoke), CLAUDE.md files, ADRs, spec template, session-start hook.
- **Spike A (netcode):** Godot .NET + Backdash + toy sim (two squares); SyncTest in CI; two instances over UDP with simulated lag.
- **Spike B (art):** VRoid model → Blender → Godot toon shader, 5 stepped animations, one palette swap.
  Tests whether Claude + Blender MCP + learning key-pose animation is viable.
- **Spike C (Steam):** Steam Networking Sockets adapter using test AppID 480 (no fee needed yet).
- **Exit:** ADR per spike; go/no-go on Backdash and on the art pipeline (no-go → decide on outsourcing; gameplay work is not blocked).

> **[GAP-1] Repo & solution layout.** Is the simulation a pure C# class library with no Godot dependency (so it can be
> unit-tested, run headless and soak-tested without the engine), with Godot only as a presentation/host layer? This is
> the most important structural decision and it isn't written down. We also need the project list (e.g. `Sim`,
> `Sim.Tests`, `Netcode`, `Platform.Steam`, `Game` (Godot), `Tools`) and the allowed dependency directions.
>
> **[GAP-2] Pinned toolchain.** No Godot version (4.x minor, .NET variant), .NET SDK/TFM, C# language version or
> Backdash version is pinned, and there is no engine upgrade policy. Godot .NET has platform export limits, so
> confirm Windows + Linux export works with the chosen version.
>
> **[GAP-3] Determinism contract.** "Integer math" isn't a spec. Define: the fixed-point format (e.g. Q16.16 vs
> int64 subpixels); sim units (e.g. 1 px = 1000 units, stage width in units); lookup tables for trig/sqrt; a
> PRNG that is seeded and stored in game state; banned APIs in the sim assembly (`float`/`double`, `System.Random`,
> `DateTime`, `Dictionary`/`HashSet` iteration order, LINQ, `async`, static mutable state), **enforced by a Roslyn
> analyzer or banned-API list in CI** rather than review discipline. Also confirm Windows ↔ Linux cross-play
> determinism (the Deck may run the native Linux build or Windows under Proton).
>
> **[GAP-4] Spike A has no quantitative pass/fail.** Add numbers. For example: max rollback window (7–8 frames);
> worst-case cost of resimulating N frames + render within the 16.67 ms budget on Steam Deck (sim step target e.g.
> < 0.5 ms for 2 characters); save/load state cost and size; checksum cost; zero allocations per sim tick (GC pauses
> break rollback).
>
> **[GAP-5] Backdash due diligence.** Licence, maintenance activity, bus factor, spectator support, input-size limits,
> custom transport API (needed for Steam), and the exit strategy if it's abandoned. Should we fork/vendor it?
>
> **[GAP-6] Game loop & frame pacing.** Godot's `_PhysicsProcess` / `_Process` are not a rollback-safe fixed-step
> loop. Specify: who owns the 60 Hz tick; when input is polled relative to the tick (latency!); behaviour on
> 120/144 Hz monitors and VRR; vsync / frame-pacing policy; what happens when a frame is late (skip, slow down, time
> sync). Snopek's pitfalls note belongs here. **Needed by: Phase 0 (Spike A).**
>
> **[GAP-7] Spike B: undefined viability criteria and pipeline specifics.** "Viable" needs measurable criteria (hours
> per animation, quality bar judged against a reference frame from Xrd). Also missing: VRoid licence terms for
> commercial use and redistribution of derived models; how edited/custom normals survive Blender → glTF → Godot import;
> how stepped (constant interpolation) animation is authored and exported; **how animation frames are sampled from the
> sim frame number** (not delta time) so visuals stay in sync after rollback; and where hitboxes are authored relative
> to animations.
>
> **[GAP-8] Spike C: Steam binding choice.** Steamworks.NET vs Facepunch.Steamworks vs GodotSteam (.NET): pick one in
> an ADR. Also cover: Steam Datagram Relay (hides IPs, adds latency?) vs direct P2P, unreliable vs reliable channels
> for inputs, NAT traversal fallback, and AppID 480 limitations.
>
> **[GAP-9] CI specifics.** GitHub Actions runners have no GPU. We need a Godot headless .NET setup, cached export
> templates, test frameworks (xUnit/NUnit for the sim, GdUnit4 or similar for Godot), a Linux + Windows matrix (to
> catch cross-platform determinism drift), CI time budgets, and a way for cloud Claude sessions to run the same checks.

### Phase 1 — Simulation core, placeholder art, offline

- Integer math, entities, input layer (devices, remapping, SOCD) + motion parser, move state machine, base character,
  box collision, hit/block, hitstun/blockstun/hitstop, health, rounds, walls/corners, camera.
- Placeholder character "Testman" (~10 moves), hitbox overlay, scenario tests, 10k-frame SyncTest in CI.
- **Exit:** both devs play local versus on controllers and it already feels responsive; CI green.

> **[GAP-10] Character / move data format.** Define the schema before any moves are written: frame data
> (startup/active/recovery), per-frame hit/hurt/push/throw boxes, cancel windows, hit properties, and the move
> state machine definition. Also define the storage format (Godot `Resource`? JSON? custom binary?), how it's baked
> into integer sim data, its versioning/migration rules, and validation. The Phase 3 editor and Phase 8 replay
> versioning both depend on it. **Needed by: Phase 1.**
>
> **[GAP-11] Input layer spec.** SOCD modes to support (last-input-priority, neutral, up-priority; per-device default);
> input buffer length and per-motion leniency windows; motion priority when several match on the same frame
> (e.g. 236 vs 623 vs 41236, special button vs motion); how Steam Input and the raw joypad backend coexist without
> double inputs; and the input frame format that's sent over the network (bitfield size).
>
> **[GAP-12] Hit resolution rules.** Same-frame trades, P1/P2 processing order (must be symmetric — no port bias),
> multi-hit rules, hit vs throw priority, throw vs throw clashes, pushbox resolution against walls and in the corner,
> crossup side switching. All of this has to be deterministic and documented.
>
> **[GAP-13] Camera is gameplay.** The max distance between players and wall/corner behaviour depend on the camera, so
> the camera's horizontal position must live in the **sim**, with zoom/shake in presentation. Currently it's
> listed without a side.
>
> **[GAP-14] "Feels responsive" is not measurable.** Define a latency target (input-to-photon, e.g. ≤ 4 frames
> local on a 60 Hz display, benchmarked against Strive) and how it's measured (high-speed camera / LED input lag
> tester). Add it as a tracked metric.
>
> **[GAP-15] Scenario test format.** How a scenario is written (input script + expected state assertions? golden
> replay + checksum?) so Claude sessions and humans can both write them. Add a **golden-replay regression suite**:
> recorded matches whose per-frame checksums must not change unless intentionally re-blessed.

### Phase 2 — Online

- Backdash integration behind our sessions; P2P over UDP, then over Steam (minimal lobby / friend invite).
- Network simulator panel, desync detection + dumps, replays, input delay settings.
- **Exit:** play each other online over Steam from different networks, feels good at ~100 ms; 1-hour bot-vs-bot soak without desync.
- Online comes this early to validate the architecture while the codebase is still small.

> **[GAP-16] Session abstraction API.** "Behind our sessions" needs an interface spec (local, P2P, SyncTest, replay,
> spectator) so Backdash stays swappable (see GAP-5).
>
> **[GAP-17] Rollback presentation side effects.** VFX, SFX, voice lines, screen shake and controller rumble
> triggered during resimulation must not double-fire or linger after a rollback. We need a strategy (e.g.
> events keyed by frame + id, reconciled after rollback). This is unaddressed and hard to retrofit. **Needed by:
> Phase 2.**
>
> **[GAP-18] Network parameters.** Default input delay and the auto-delay algorithm, max rollback frames, time-sync /
> frame-advantage correction, and the disconnect timeout and what it means (round result? match void?). Also: pause
> behaviour online, a connection quality indicator, and the exact meaning of "~100 ms" (RTT, not one-way?). Add a
> test matrix for jitter and packet loss (e.g. 100 ms RTT, ±20 ms jitter, 2 % loss).
>
> **[GAP-19] Desync tooling spec.** Checksum algorithm and frequency, what a desync dump contains (state per frame
> from both peers, inputs, build hash), how dumps are collected from players (upload? clipboard?), and a
> diff/bisect tool.
>
> **[GAP-20] Version handshake.** Peers must verify identical build/sim/data hashes, plus identical match config
> (rules modules, variants — see GAP-24) before a match starts.
>
> **[GAP-21] Replay format.** Header (build hash, data version, config, characters, palettes, seed), input stream,
> compression, file extension, and a forward-compatibility strategy (see Phase 8).
>
> **[GAP-22] Soak test infrastructure.** Where does a 1-hour bot-vs-bot run happen (CI job or nightly)? Over a
> simulated in-process transport with injected loss? What determines the bot inputs (seeded random for
> reproducibility)?
>
> **[GAP-23] Security / privacy baseline.** Raw UDP P2P leaks IP addresses (use Steam relay by default?), validation of
> malformed packets, and a basic threat model (input-only sync limits cheating, but not macros, lag switching or
> result spoofing — relevant for ranked).

### Phase 3 — Systems & the Lab

- Full module system: gatlings, counter/punish counter, proration, meter + supers, throws/tech, pushback guard,
  instant block, burst, Roman Cancel v1 + switchable variants, special button + dash button behaviour.
- Training mode (frame meter, record/playback, savestates), control panel, character editor v1, simple CPU bot.
- **Exit:** Testman has the full system kit; a new move can be created in the editor without code; variants switch live.

> **[GAP-24] "Module system" is undefined.** What is a module (an interface? a rules component in the sim pipeline?),
> how its execution order is fixed and deterministic, how its config is represented, and how "switch live" works
> without breaking state (only between rounds? part of game state so it rolls back?). Online, module config must be
> negotiated and checksummed (GAP-20).
>
> **[GAP-25] Sim introspection for the frame meter.** The frame meter needs the sim to expose per-frame state
> classification (startup/active/recovery/stun/invuln/throw-invuln) in a defined format.
>
> **[GAP-26] Savestates = rollback serialization?** Confirm that training savestates reuse rollback snapshots, and
> define whether they persist to disk (then they need versioning).
>
> **[GAP-27] Character editor architecture.** Is it a Godot editor plugin, an in-game tool or a standalone app? How it
> reads/writes the GAP-10 format, hot reload into a running match, validation (e.g. no active frames without
> hitboxes), undo, and merge-friendliness of the data files for two devs + Claude editing in parallel.
>
> **[GAP-28] CPU bot architecture.** Does it run as an input provider outside the sim, so it's reusable for soak tests
> and replays (deterministic if seeded)? Will it use scripted, state-machine or utility AI? Does it have a
> reaction-time model?

### Phase 4 — Vertical slice (most important milestone)

- First real character with final-quality art, final toon shader, stage 1, hit VFX, SFX, HUD.
- Game feel tuning pass: hitstop, screen shake, camera.
- **Exit:** online mirror match that genuinely feels great; external FGC playtest passes.
- Then: Steamworks partner account, $100 app fee, store art + short trailer → Steam "Coming Soon" page live *(timing proposal)*.

> **[GAP-29] Performance budgets (missing for the whole project).** Define these per frame on the min-spec target
> (Steam Deck): CPU ms for sim × rollback window, render ms, draw calls, triangles and bones per character,
> texture memory, VRAM, load times, RAM. Also a **min-spec PC** definition. Without budgets, "perf on Steam Deck"
> in Phase 7 is too late. **Needed by: Phase 4 (start measuring in Phase 1).**
>
> **[GAP-30] Toon shader technical spec.** Godot renderer choice (Forward+ vs Mobile vs Compatibility, as it affects
> Deck performance); lighting model (per-character fixed light direction, shadow thresholds, vertex-colour / ILM-style
> control textures); outline method and width scaling with camera distance; anti-aliasing; how palette textures
> map onto materials.
>
> **[GAP-31] Audio architecture.** Godot built-in audio vs FMOD/Wwise (licensing!), bus layout, rollback-safe playback
> (GAP-17), voice/SFX priorities and limits, and loudness standard (e.g. −14/−16 LUFS).
>
> **[GAP-32] VFX authoring pipeline.** 2D sprite effects vs particles, how they share the toon colour treatment, and
> how they're positioned from sim data (hit spark at hitbox intersection point).
>
> **[GAP-33] "FGC playtest passes" has no criteria.** Define a survey/scorecard (input feel, readability,
> netcode feel at given ping) and a pass threshold.

### Phase 5 — Roster & content

- Characters 2–4 (each proves new engine features), 2–3 stages, music, palettes/costumes,
  menus, character select, settings, rebinding UI.
- **Exit:** launch content complete; every character passes the uniqueness check.

> **[GAP-34] Asset pipeline & large files.** Git LFS (GitHub LFS storage/bandwidth costs, and cloud Claude sessions
> cloning LFS) vs a separate asset store; folder/naming conventions, Godot import presets, and source-art
> (`.blend`) vs exported-art storage. **Needed by: Phase 0** (art lands in the repo with Spike B).
>
> **[GAP-35] Save data & settings persistence.** File locations per OS, format, versioning/migration, and which files
> go to Steam Cloud (Phase 6). This includes keybinds per device and video/audio settings.
>
> **[GAP-36] Localization & fonts.** Not mentioned anywhere. Which languages ship? String table system, and fonts with
> CJK coverage (FGC audience is heavily JP/KR/CN), text expansion in the UI. Retrofitting this late is expensive.
>
> **[GAP-37] Accessibility.** Colour-blind-safe hit/guard feedback, remappable everything, text size (overlaps with Deck
> readability), screen-shake/flash toggles (photosensitivity).
>
> **[GAP-38] "Uniqueness check" is undefined.** Write down the checklist.

### Phase 6 — Online features & Steam integration

- Lobby UI (public/private), quick match via Steam matchmaking, rematch, replay browser.
- Achievements, cloud saves, Steam Input, Steam Deck requirements (button glyphs, readable text at 1280×800, locked 60 fps).
- CI exports Windows + Linux builds and uploads to a Steam beta branch automatically (SteamPipe / steamcmd).
- **Exit:** closed beta via Steam Playtest.

> **[GAP-39] Quick match algorithm.** Steam lobby search filters, ping-based filtering (Steam's ping location API),
> region handling, and the fallback when the pool is small (a common problem for indie FGs). Plan for low
> player counts explicitly.
>
> **[GAP-40] Steam Deck Verified details.** Native Linux build vs Proton on Deck (affects GAP-3), suspend/resume during
> an online match, the default Steam Input config and glyph API, on-screen keyboard for text input, and
> no-launcher requirements.
>
> **[GAP-41] Release engineering.** Version numbering scheme, branching/release strategy, build signing (Windows
> code signing?), steamcmd credentials in CI (dedicated build account, Steam Guard handling, secret storage),
> debug symbol storage, and how to roll back a bad Steam build.
>
> **[GAP-42] Crash reporting & telemetry.** Not planned at all. Crash dump capture for .NET + native Godot crashes
> (e.g. Sentry/Backtrace), opt-in gameplay/netcode telemetry (rollback frames, desync rate, RTT distribution), and
> **GDPR**: privacy policy and consent, since we're EU-based. **Needed by: Phase 6 (closed beta needs crash data).**

### Phase 7 — Beta, polish, launch

- Open Playtest; balance + netcode fixes from feedback; performance on Steam Deck.
- Steam Next Fest demo — **OPEN** (Next Fest runs Feb/Jun/Oct; registration months ahead).
- Release review (a few days), 1.0 launch.

> **[GAP-43] Live-ops / patch process.** Hotfix flow, how online enforces "same build" across a patch rollout, and
> communicating breaking changes (replays, savestates).
>
> **[GAP-44] Demo build technicals.** If yes to Next Fest: a separate AppID, content stripping (no unshipped
> characters in the build), and demo-specific build config in CI.

### Phase 8 — Post-launch

- Ranked, spectators, additional (paid) characters, balance patches.
- Replays record their game version; old replays play only on that version or are marked unsupported.

> **[GAP-45] Backend for ranked.** P2P result reporting can be spoofed. Ranked needs either a backend service
> (hosting, cost, auth via Steam session tickets, rating algorithm e.g. Glicko-2, ops responsibility for a 2-person
> team) or acceptance of Steam leaderboards' weaker guarantees. This is the only server component in the whole plan,
> and it's currently implicit.
>
> **[GAP-46] Spectator architecture.** Input broadcast with delay, relayed via a host peer or Steam? Does Backdash
> support it (GAP-5)? How many spectators?
>
> **[GAP-47] DLC & online compatibility.** Every client must contain every character's data (and probably its
> assets) to play against DLC owners. Only *selection* is entitlement-gated. This affects content packaging from
> Phase 5 on, so decide it early.
>
> **[GAP-48] Replay compatibility strategy.** "Plays only on that version" implies either shipping multiple sim
> versions, Steam beta branches per old version, or converting replays to video. Pick one; the GAP-10 data
> versioning must support it.

### Cross-cutting

- Art track in parallel from Phase 2 onward (art is the main bottleneck), once Spike B says go.
- Weekly playtests; docs updated with code; ADRs for decisions.

> **[GAP-49] Test strategy overview.** One document tying together unit tests (sim), scenario tests, golden replays
> (GAP-15), SyncTest, soak (GAP-22), perf regression tests (GAP-29), and Godot smoke tests, with what runs on PR vs
> nightly.
>
> **[GAP-50] Logging & diagnostics.** Structured logging that's safe inside the sim (zero allocations, no I/O in the tick),
> log levels per build type, and an in-game debug overlay spec (beyond the hitbox overlay).
>
> **[GAP-51] Third-party licence audit.** Track the licence of every dependency and asset (Backdash, Steam
> binding, VRoid, fonts, audio middleware, shaders) in one file, checked before release.
>
> **[GAP-52] Claude-in-the-loop guardrails.** Cloud sessions writing sim code: CI must catch determinism violations
> (GAP-3 analyzer), data-format breaks (GAP-10 validation) and perf regressions (GAP-29) automatically, since
> humans won't spot them in review.

---

## 8. Open questions summary

1. Signature hook mechanic.
2. Setting, tone, name.
3. Attack button layout.
4. Roman Cancel design + alternatives.
5. Steam Next Fest demo: yes/no.
6. Store page timing (proposal: right after the vertical slice).
7. Final roster size and characters (after core works; proposal: 4 at launch).
8. Art pipeline viability (answered by Spike B).

> **[GAP-53] The open questions are all design/business ones.** Add the technical open questions: Steam binding
> (GAP-8), fixed-point format (GAP-3), data format (GAP-10), audio middleware (GAP-31), Godot renderer (GAP-30),
> ranked backend (GAP-45), DLC packaging (GAP-47).
>
> Note: the attack button layout (Q3) also blocks the input frame format (GAP-11) and Testman's move list, so it
> should be decided before Phase 1, not just "consulting Robin".

---

## 9. Technical gap index

Ordered by when the decision blocks work. Each gap should become an ADR or spec.

| Needed by | Gaps | Why it blocks |
|---|---|---|
| **Phase 0** | 0, 1, 2, 3, 4, 5, 6, 8, 9, 34 | Architecture, determinism and loop timing shape every line of sim code; spikes need pass/fail numbers; art enters the repo in Spike B. |
| **Phase 0 (Spike B)** | 7 | Art go/no-go is meaningless without criteria and licence clearance. |
| **Phase 1** | 10, 11, 12, 13, 14, 15, 52, Q3 | Move data format and input/hit rules are the core sim contracts; retrofitting them means rewriting Testman. |
| **Phase 2** | 16, 17, 18, 19, 20, 21, 22, 23 | Rollback side effects and version handshake are painful to retrofit. |
| **Phase 3** | 24, 25, 26, 27, 28 | Module system and editor architecture. |
| **Phase 4** | 29, 30, 31, 32, 33 | Budgets and the render/audio stack must be fixed before final-quality art is produced. |
| **Phase 5** | 35, 36, 37, 38, 47 | Localization and DLC packaging decisions affect all content made from here. |
| **Phase 6** | 39, 40, 41, 42 | Beta needs crash reporting, release engineering and GDPR compliance. |
| **Phase 7–8** | 43, 44, 45, 46, 48 | Live ops, ranked backend, spectators, replay compatibility. |
| **Continuous** | 49, 50, 51, 53 | Test strategy, diagnostics, licences, technical open questions. |

**Highest-risk gaps (tackle first):** GAP-0/1 (architecture), GAP-3 (determinism enforcement), GAP-6 (game loop &
input timing), GAP-10 (move data format), GAP-17 (rollback side effects), GAP-29 (perf budgets).
