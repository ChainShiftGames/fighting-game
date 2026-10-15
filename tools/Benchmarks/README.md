# Benchmarks

Speed and allocation benchmark for the game logic (ADR-0003, P0-14). Measures one tick, a resimulation of the
rollback window (restore a snapshot + 8 ticks, the rollback worst case), snapshot save and load, and the checksum.
Inputs are a fixed seeded sequence, so every run measures the same frames.

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

Exit codes: `0` passed, `1` a gate failed (details on stderr), `2` invalid arguments.

## How it measures

- Each sample times a batch of calls sized to last ~100 µs, because one toy call is far below the timer's
  resolution. Mean, median and p99 are per call, over batch averages; a single slow call is smoothed out.
- Allocations are counted over all timed calls. Any byte allocated fails the run.

## CI gates

The `Benchmark` step in `.github/workflows/ci.yml` runs `--max-tick-us 1000`:

- **Allocation (hard gate):** any measured operation that allocates fails CI (ADR-0003: no allocation per frame).
- **Time (loose gate):** the tick's p99 must stay under 1000 µs on the GitHub runner. This only catches gross
  regressions. Shared CI VMs vary in speed between runs and aren't a Steam Deck, so a tight limit would flake and
  still not prove the Deck budget. The real ≤ 1 ms check is the manual Deck run below, at each milestone.

## Steam Deck run (manual)

1. On the dev PC: `dotnet publish tools/Benchmarks -c Release -r linux-x64 --self-contained`
2. Copy `tools/Benchmarks/bin/Release/net10.0/linux-x64/publish/` to the Deck (e.g. `scp -r`); no .NET install
   needed on the Deck.
3. On the Deck, in Desktop Mode, plugged in: `chmod +x Benchmarks && ./Benchmarks`
4. Add the printed row values to the table below, with the date and commit.

## Results

Toy simulation (P0-10), 8-frame rollback window. Times in µs per call.

| Machine | Operation | Mean µs | Median µs | p99 µs | Bytes/call |
|---|---|---:|---:|---:|---:|
| Windows dev PC (i7-12700F, Windows 11, .NET 10.0.13), 2026-10-15 | Tick | 0.0036 | 0.0031 | 0.0124 | 0 |
| | Resimulate 8 frames | 0.0361 | 0.0341 | 0.0753 | 0 |
| | Snapshot save | 0.0056 | 0.0054 | 0.0099 | 0 |
| | Snapshot load | 0.0041 | 0.0040 | 0.0070 | 0 |
| | Checksum | 0.0142 | 0.0131 | 0.0279 | 0 |
| Steam Deck | Tick | | | | |
| | Resimulate 8 frames | | | | |
| | Snapshot save | | | | |
| | Snapshot load | | | | |
| | Checksum | | | | |

The toy tick is ~3 ns, so the 1 ms budget isn't under pressure yet; the numbers matter again once Phase 1 adds
real fighters and modules.
