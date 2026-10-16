# ADR-0016: Game tests may reference game/

- **Status:** Accepted (Oct 2026; both devs approved on the P1-04 PR)

## Context

ARCHITECTURE.md §2 let tests reference only `Sim`, `Sim.Data` and `Netcode`, so nothing in `game/` could have unit
tests. P1-04 (device layer) has logic worth testing without hardware: bindings → input word, both opposite
direction bits kept, the bindings file, joypad slots. Hardware checks alone would leave it unguarded.

## Decision

1. **One test project, `tests/FightingGame.Tests`, may reference `game/FightingGame.csproj`.** No other project in
   `tests/` or `tools/` may (checked by `Architecture.Tests`).
2. **It tests only code that doesn't call into the Godot engine.** The tests run without Godot (CI has none), so
   they may use Godot's plain types (`Key`, `JoyButton` enums) but nothing that needs the engine running. Code that
   reads devices sits behind a small interface (`IDeviceState`) so the logic around it is testable.
3. **Only `game/` and its tests use Godot types.** Every other runtime assembly stays engine-free (checked by
   `Architecture.Tests` for `Sim`, `Sim.Data`, `Netcode`, `Platform.Steam`).

## Consequences

- `game/` code can get unit tests; scenes and engine calls are still checked by hand or with the Godot editor.
- Dropping this: delete `tests/FightingGame.Tests`, its solution entry, this ADR and its lines; the game builds and
  runs unchanged.
