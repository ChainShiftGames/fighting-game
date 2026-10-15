# ADR-0001: Game logic is a pure C# library; toolchain versions are pinned

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-1, GAP-2

## Context

Rollback netcode needs game logic that is deterministic, fast to save/load and replay, and testable without
running the engine. Godot's node tree, physics and frame callbacks don't give us any of that.

## Decision

1. All game logic (the "sim": movement, moves, hits, rounds, meter, mechanics) lives in a plain C# class library
   with **no reference to Godot**.
2. Godot is the host and presentation layer only: it reads input devices, calls the sim once per logic frame,
   and draws and plays audio from the sim's state and events.
3. Dependencies point one way: `Game (Godot)` → `Netcode` → `Sim`. The sim never depends on anything above it.
4. Versions are pinned: the latest stable Godot 4.x .NET release at Spike A time, plus the .NET LTS SDK it
   supports (in `global.json`). The exact numbers are added here at Spike A. Engine and SDK upgrades are
   deliberate, separate PRs.

## Pinned versions (P0-01, Oct 2026)

| Tool | Version | Pinned in |
|---|---|---|
| Godot | 4.7.2-stable, .NET build (18 Aug 2026) | this ADR; editor + export templates |
| Godot.NET.Sdk / GodotSharp | 4.7.2 | `game/*.csproj` (P0-02) |
| .NET SDK | 10.0.402 (LTS, runtime 10.0.13), `rollForward: latestPatch` | `global.json` |
| Target framework | `net10.0` for every project | `Directory.Build.props` (P0-02) |
| C# | 14 | `LangVersion` in `Directory.Build.props` (P0-02) |
| Backdash | 0.7.8-preview (MIT) | `src/Netcode/*.csproj` (P0-11) |

- .NET 10 is the only LTS supported past Nov 2026 (.NET 8 and 9 both end 10 Nov 2026). Godot 4.5+ needs .NET 8+;
  GodotSharp 4.7.2 targets net8.0 and is consumed from net10.0.
- `latestPatch` accepts newer 10.0.4xx patch SDKs only; a new feature band is a deliberate upgrade PR.
- Backdash has no stable release yet. 0.7.8-preview (Jul 2026, net8.0) was chosen over 0.8.0-preview (Oct 2026,
  net10.0, one day old at pin time) for maturity. Revisit at the Spike A go/no-go (P0-15).
- Windows and Linux export with this Godot version is confirmed by the nightly headless export job (P0-06).

## Consequences

- Sim tests, determinism tests and bot soak tests run headless and fast in CI, without Godot.
- Presentation code can't "accidentally" change gameplay.
- We write our own small math, collision and state-machine code instead of using Godot's.
- Data the sim needs (characters, moves) must be in an engine-neutral format (see GAP-10).
