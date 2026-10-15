# Fighting Game — Roadmap & Open Questions

> Companion to [CURRENT_STATE.md](CURRENT_STATE.md).

- Still to review later: Ikemen GO / MUGEN character format, Snopek's Godot rollback addon (frame-timing pitfalls),
  GDC "8 Frames in 16ms" (NetherRealm rollback), GDC 2015 Guilty Gear Xrd art talk, Infil's netcode article, Dustloop wiki.

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

### Phase 1 — Simulation core, placeholder art, offline

- Integer math, entities, input layer (devices, remapping, SOCD) + motion parser, move state machine, base character,
  box collision, hit/block, hitstun/blockstun/hitstop, health, rounds, walls/corners, camera.
- Placeholder character "Testman" (~10 moves), hitbox overlay, scenario tests, 10k-frame SyncTest in CI.
- **Exit:** both devs play local versus on controllers and it already feels responsive; CI green.

### Phase 2 — Online

- Backdash integration behind our sessions; P2P over UDP, then over Steam (minimal lobby / friend invite).
- Network simulator panel, desync detection + dumps, replays, input delay settings.
- **Exit:** play each other online over Steam from different networks, feels good at ~100 ms; 1-hour bot-vs-bot soak without desync.
- Online comes this early to validate the architecture while the codebase is still small.

### Phase 3 — Systems & the Lab

- Full module system: gatlings, counter/punish counter, proration, meter + supers, throws/tech, pushback guard,
  instant block, burst, Roman Cancel v1 + switchable variants, special button + dash button behaviour.
- Training mode (frame meter, record/playback, savestates), control panel, character editor v1, simple CPU bot.
- **Exit:** Testman has the full system kit; a new move can be created in the editor without code; variants switch live.

### Phase 4 — Vertical slice (most important milestone)

- First real character with final-quality art, final toon shader, stage 1, hit VFX, SFX, HUD.
- Game feel tuning pass: hitstop, screen shake, camera.
- **Exit:** online mirror match that genuinely feels great; external FGC playtest passes.
- Then: Steamworks partner account, $100 app fee, store art + short trailer → Steam "Coming Soon" page live *(timing proposal)*.

### Phase 5 — Roster & content

- Characters 2–4 (each proves new engine features), 2–3 stages, music, palettes/costumes,
  menus, character select, settings, rebinding UI.
- **Exit:** launch content complete; every character passes the uniqueness check.

### Phase 6 — Online features & Steam integration

- Lobby UI (public/private), quick match via Steam matchmaking, rematch, replay browser.
- Achievements, cloud saves, Steam Input, Steam Deck requirements (button glyphs, readable text at 1280×800, locked 60 fps).
- CI exports Windows + Linux builds and uploads to a Steam beta branch automatically (SteamPipe / steamcmd).
- **Exit:** closed beta via Steam Playtest.

### Phase 7 — Beta, polish, launch

- Open Playtest; balance + netcode fixes from feedback; performance on Steam Deck.
- Steam Next Fest demo — **OPEN** (Next Fest runs Feb/Jun/Oct; registration months ahead).
- Release review (a few days), 1.0 launch.

### Phase 8 — Post-launch

- Ranked, spectators, additional (paid) characters, balance patches.
- Replays record their game version; old replays play only on that version or are marked unsupported.

### Cross-cutting

- Art track in parallel from Phase 2 onward (art is the main bottleneck), once Spike B says go.
- Weekly playtests; docs updated with code; ADRs for decisions.

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
