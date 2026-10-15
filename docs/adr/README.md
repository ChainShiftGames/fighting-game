# ADR index

One line per decision. Read the full ADR only when it's relevant to your task.

| ADR | Covers |
|---|---|
| [0001](0001-pure-csharp-simulation.md) | Game logic is engine-free C#; dependency direction; pinned Godot/.NET versions |
| [0002](0002-input-defaults.md) | SOCD default (player setting), 2 f button buffer, 12 f motion window (developer settings) |
| [0003](0003-determinism-and-sim-performance.md) | Integers only, banned APIs, 8-frame rollback, ≤ 1 ms tick, no allocations |
| [0004](0004-game-loop-and-camera.md) | Own 60 Hz loop, input read timing, vsync, latency counter, camera as gameplay |
| [0005](0005-online-architecture.md) | Steamworks.NET + relay, match drivers, rollback effects, delay, desyncs, handshake, replays, match rules |
| [0006](0006-testing-and-ci.md) | Minimal CI, plain-text scenario tests, golden replays, on-demand soak test |
| [0007](0007-character-data-and-editor.md) | JSON character data, Godot editor plugin, frame meter data, in-memory savestates |
| [0008](0008-presentation-direction.md) | Placeholders first, visual toggles, target look, Godot audio, accessibility incl. directional audio |
| [0009](0009-architecture-tests.md) | ArchUnitNET tests enforcing dependency and module rules |
| [0010](0010-attack-button-layout.md) | A B C D + Special + Dash, button set in data, 16-bit input word with spare bits |

New ADR: copy the format of an existing one, use the next number, add a line here.
