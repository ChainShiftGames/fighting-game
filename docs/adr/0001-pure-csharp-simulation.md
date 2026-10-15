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

## Consequences

- Sim tests, determinism tests and bot soak tests run headless and fast in CI, without Godot.
- Presentation code can't "accidentally" change gameplay.
- We write our own small math, collision and state-machine code instead of using Godot's.
- Data the sim needs (characters, moves) must be in an engine-neutral format (see GAP-10).
