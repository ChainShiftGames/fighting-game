# ADR-0013: Spike A: keep Backdash for rollback

- **Status:** Proposed (Oct 2026) <!-- Accepted once both devs approve (P0-15) -->

## Context

Spike A (P0-10 to P0-14) had to show that rollback with Backdash works with our setup before Phase 1 builds on
it. ADR-0001 pinned Backdash 0.7.8-preview and asked for this go/no-go. Rules it is measured against: ADR-0003
(determinism, ≤ 1 ms logic frame, no allocation), ADR-0005 (one match-driver interface, test conditions
100 ms / ±20 ms / 2 %).

## Measurements

- **Logic speed (P0-14, toy sim, 8-frame window):** dev PC (i7-12700F) tick 0.0036 µs mean / 0.0124 µs p99,
  8-frame resimulation 0.036 µs, snapshot save 0.006 µs, load 0.004 µs, checksum 0.014 µs; **0 bytes allocated**
  in all of them (CI gate). Steam Deck: **not measured yet**. Details: `tools/Benchmarks/README.md`.
- **Determinism (P0-11):** Backdash SyncTest, 10,000 frames of seeded random input, 8-frame check distance:
  passes in CI. A deliberate determinism bug is caught (`DesyncDetectedException`).
- **Peers over UDP (P0-13, `PeerSessionTests`):** two peers on loopback at 100 ms / ±20 ms / 2 %, 900 frames of
  random input: identical final state, 3 of 3 runs. A non-deterministic peer is caught by Backdash's consistency
  check.
- **Playtest (P0-13):** both devs, two PCs over the internet (Tailscale direct link, ~36 ms real ping) plus the
  simulated 100 ms / ±20 ms / 2 %: **10+ minutes, no desync**. Feel: "little jitters here and there" with the
  added lag, "within bounds of what we expect from other AAA fighting games"; very smooth without it.
- **Input latency (P0-12, not Backdash):** input to draw 0.3 ms with vsync off, ~16 ms with vsync on (Godot's
  swapchain frame queue).

## Problems found

1. **Preview only.** No stable Backdash release; 0.8.0-preview (net10.0) is out. Our use is wrapped in three
   classes (`RollbackMatchDriver`, `SyncTestSession`, `PeerSession`) plus `Netcode/Transport/`.
2. **Input type:** the plain `WithInputType<T>()` only takes enums; we use `Struct<InputWord>()` (raw bytes). Fine
   while every target is little-endian x64.
3. **No packet-loss simulation** (latency and jitter only). We wrote `LagSimulatingSocket` at Backdash's
   `IPeerSocket` boundary, the same point the Steam relay transport (P0-21) uses.
4. **Two checksums:** Backdash compares its own 32-bit Fletcher over the saved bytes; our `StateChecksum` is 64-bit
   FNV-1a. Both hash the same bytes, so they agree on "same state"; desync dumps (Phase 2) should use ours.
5. **Not measured:** Backdash's own cost and allocations per frame (input sync, saving states, network). The
   benchmark covers only our logic.
6. Minor: time sync hands over frames to wait (the driver skips them); Backdash logs a harmless "prediction
   barrier" warning when a peer runs 8 frames ahead.

## Decision

1. **Go: Backdash stays our rollback library**, behind `IMatchDriver` (ADR-0005 §2).
2. **No fork, no vendoring.** Use the NuGet package pinned in ADR-0001. Fork only for a blocking bug that upstream
   won't fix in time; then vendor the fork as its own ADR.
3. **Stay on 0.7.8-preview through Phase 1.** Moving to 0.8.x is a separate PR, gated by the SyncTest and
   `PeerSessionTests`.
4. **Keep the lag-simulating transport** for development; its values are **developer settings**
   (`NetworkSimulation`).

## Consequences

- Phase 1 builds the real simulation on the current drivers; SyncTest and the peer tests stay in every PR.
- Follow-ups before the Phase 0 exit:
  - Steam Deck benchmark run (P0-14 manual step); add its numbers here.
  - Measure Backdash's per-frame cost and allocations through `RollbackMatchDriver`, in the benchmark.
- Later: tune Godot's frame queue for vsync latency (ADR-0004 §6), automatic input delay (ADR-0005 §4), desync
  dump files (ARCHITECTURE.md §8).
