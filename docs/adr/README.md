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
| [0011](0011-module-contract.md) | Module contract, reserved core capabilities (time scale, freeze, cancel, hitstop, entities, gravity, stun, module events; move lock and meter gauge proposed), air guard, cancel window and hit adjustment questions, `MatchRules` struct + hash test, module lineup + toggles in data |
| [0012](0012-dev-and-test-dependencies.md) | xUnit v3 on Microsoft.Testing.Platform; Godot AI editor addon vendored dev-only, excluded from exports, telemetry off |
| [0013](0013-spike-a-backdash-go.md) | Spike A: Backdash stays (no fork) on 0.7.8-preview through Phase 1; measurements, problems found, follow-ups |
| [0014](0014-spike-c-steam-p2p-go.md) | Spike C: Steam Networking Messages stays; Steam picks direct (friends) or relay; measurements, problems, follow-ups |
| [0015](0015-shipping-game-data.md) | Exports read `data/**/*.json` embedded in `Sim.Data.dll`; editor and tests read the `data/` folder |
| [0016](0016-game-tests.md) | `tests/FightingGame.Tests` may reference `game/` for engine-free tests; only `game/` and its tests use Godot |
| [0017](0017-core-throws-counter-hits.md) | Throws (with tech) and counter hits are core, always present; air block is a removable module |
| [0018](0018-replay-format.md) | Replay file layout, same-build-only playback, full `MatchRules` in the header, recording around the game logic |
| [0019](0019-desync-checksum-link.md) | Desync checksums ride the match transport as a second message kind; unreliable, repeated until acknowledged |
| [0020](0020-special-button-shortcuts.md) | Special button shortcuts (direction + special) are core move input; damage and cooldown trade-off in `MatchRules` |

New ADR: copy the format of an existing one, use the next number, add a line here.
