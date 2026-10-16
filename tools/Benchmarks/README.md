# Benchmarks

Speed and allocation benchmark for the game logic (ADR-0003, P0-14). Measures one tick, a resimulation of the
rollback window (restore a snapshot + 8 ticks, the rollback worst case), snapshot save and load, and the checksum.
Inputs are a fixed seeded sequence, so every run measures the same frames.

With `--backdash` it also times whole frames through `RollbackMatchDriver` and Backdash (ADR-0013): a SyncTest
session and two peer sessions over loopback UDP. Those are recorded only, never gated.

Hand-rolled on `Stopwatch` and `GC.GetAllocatedBytesForCurrentThread()`; no benchmark package has an ADR.

## Run it

```
dotnet run --project tools/Benchmarks -c Release
```

Always Release: a Debug build prints a warning and its numbers are meaningless.

Options (developer settings):

| Option | Default | Meaning |
|---|---|---|
| `--max-tick-us <µs>` | none | Fail if the tick's p99 exceeds this. Without it, times are only reported. |
| `--rollback-frames <n>` | 8 | Frames re-run by the resimulation and slots in the snapshot ring (ADR-0003). |
| `--samples <n>` | 2000 | Timed samples per operation. |
| `--warmup-ms <ms>` | 500 | Warm-up per operation, so the JIT has finished optimising before timing starts. |
| `--backdash` | off | Also measure whole frames through Backdash (below). Reported only; never fails the run. |

Exit codes: `0` passed, `1` a gate failed (details on stderr), `2` invalid arguments.

## How it measures

- Each sample times a batch of calls sized to last ~100 µs, because one toy call is far below the timer's
  resolution. Mean, median and p99 are per call, over batch averages; a single slow call is smoothed out.
- Allocations are counted over all timed calls. Any byte allocated fails the run.

Backdash frames (`--backdash`):

- Every sample is one `TryAdvanceFrame` call, timed on its own: a frame takes microseconds, so no batching is
  needed and p99 shows single slow frames. The timer's resolution is 0.1 µs on Windows, so small values are coarse.
- **SyncTest frame:** one driver with Backdash's SyncTest session. Each frame saves and checksums the state; every
  8th frame rolls back the 8-frame window and resimulates it. The median is a plain frame, p99 a rollback frame.
- **Peer frame:** two peer sessions in this process over loopback UDP (direct UDP, no lag simulation), default
  2-frame input delay. Only P1's frames are timed; P2's frame and a 1 ms sleep run between them, so packets cross
  and Backdash's network jobs run. Paced at ~1 ms instead of 16.7 ms to keep runs short; inputs arrive in time,
  so this is the no-rollback case (SyncTest covers rollbacks).
- Allocations are counted for the whole process (Backdash's network jobs run on their own threads) and divided
  by the number of drivers. A note says how much of it came from the frame thread itself.
- Warm-up runs until the warm-up time has passed and at least 300 frames advanced, so the peers are past their
  handshake.

## CI gates

The `Benchmark` step in `.github/workflows/ci.yml` runs `--max-tick-us 1000`, without `--backdash`:

- **Allocation (hard gate):** any measured operation that allocates fails CI (ADR-0003: no allocation per frame).
- **Time (loose gate):** the tick's p99 must stay under 1000 µs on the GitHub runner. This only catches gross
  regressions. Shared CI VMs vary in speed between runs and aren't a Steam Deck, so a tight limit would flake and
  still not prove the Deck budget. The real ≤ 1 ms check is the manual Deck run below, at each milestone.
- **Backdash frames: no gate, not run in CI.** Backdash's own time and allocations aren't ours to fix
  (ADR-0013), and binding UDP ports on shared runners invites flakes. Run them by hand and record them below.

## Steam Deck run (manual)

1. On the dev PC: `dotnet publish tools/Benchmarks -c Release -r linux-x64 --self-contained`
2. Copy `tools/Benchmarks/bin/Release/net10.0/linux-x64/publish/` to the Deck (e.g. `scp -r`); no .NET install
   needed on the Deck.
3. On the Deck, in Desktop Mode, plugged in: `chmod +x Benchmarks && ./Benchmarks --backdash`
4. Add the printed row values to the table below, with the date and commit.

## Results

8-frame rollback window. Times in µs per call. Tick and resimulation run the toy simulation (P0-10) until the
real tick exists (P1-03); snapshot and checksum rows are measured for both the toy state and `MatchState` (P1-02).

| Machine | Operation | Mean µs | Median µs | p99 µs | Bytes/call |
|---|---|---:|---:|---:|---:|
| Windows dev PC (i7-12700F, Windows 11, .NET 10.0.13), 2026-10-15 | Tick (toy) | 0.0036 | 0.0031 | 0.0124 | 0 |
| | Resimulate 8 frames (toy) | 0.0361 | 0.0341 | 0.0753 | 0 |
| | Snapshot save (toy) | 0.0056 | 0.0054 | 0.0099 | 0 |
| | Snapshot load (toy) | 0.0041 | 0.0040 | 0.0070 | 0 |
| | Checksum (toy) | 0.0142 | 0.0131 | 0.0279 | 0 |
| Windows dev PC (same), 2026-10-16 | Snapshot save (MatchState, 2,400 bytes) | 0.0357 | 0.0342 | 0.0609 | 0 |
| | Snapshot load (MatchState) | 0.0203 | 0.0189 | 0.0309 | 0 |
| | Checksum (MatchState) | 2.1359 | 2.1167 | 2.5194 | 0 |
| | Tick (pipeline, P1-03: no gameplay systems yet) | 0.2933 | 0.2728 | 0.6088 | 0 |
| Steam Deck (deferred: no Deck available) | all rows | | | | |

- The toy tick is ~3 ns, so the 1 ms budget isn't under pressure yet.
- The pipeline tick (P1-03) runs every step, hook point and request application with idle stages: ~0.3 µs, the
  fixed cost every gameplay system adds to.
- `MatchState` save + load + checksum is ~2.2 µs, 0.2 % of the budget. The checksum dominates: FNV-1a goes byte
  by byte. If it ever matters, hashing 8 bytes per step is a drop-in speed-up (it changes every checksum value, so
  golden replays would need re-recording).

### Backdash frames

Whole frames through `RollbackMatchDriver<ToyState>`, toy simulation, 8-frame window. Times in µs per frame;
bytes per frame per driver, whole process.

| Machine | Operation | Mean µs | Median µs | p99 µs | Bytes/frame |
|---|---|---:|---:|---:|---:|
| Windows dev PC (i7-12700F, Windows 11, .NET 10.0.13), 2026-10-16 | SyncTest frame (8-frame check) | 0.34 | 0.2 | 1.2 | 0 |
| | Peer frame (loopback UDP) | 34.4 | 29.2 | 111.8 | 4.5 |
| Steam Deck | SyncTest frame (8-frame check) | | | | |
| | Peer frame (loopback UDP) | | | | |

- **SyncTest:** Backdash adds ~0.2 µs to a plain frame (saving and checksumming the state) and ~1 µs to a
  rollback frame. No allocation.
- **Peer:** ~30 µs per frame, 3 % of the 1 ms logic budget (which covers our logic only, not Backdash). Most of it
  is cold caches after the 1 ms sleep: pacing with a busy-wait instead gives ~6–11 µs mean. The real game is
  closer to the sleep case, since it draws between logic frames. p99 varies between runs (~110–135 µs).
- **Allocation note:** the peer sessions allocate ~4.5 bytes per frame per driver at this pace, **all on
  Backdash's background network threads; 0 bytes on the frame thread.** That is at most a few KB per second, so
  gen-0 collections stay rare; not a per-frame cost of ours. Recheck it on 0.8.x and with the Steam relay transport.
