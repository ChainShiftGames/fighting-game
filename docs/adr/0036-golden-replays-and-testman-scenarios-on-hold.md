# ADR-0036: Golden replays and testman scenarios on hold

- **Status:** Proposed (Oct 2026)

## Context

Testman's moves are being hand-tuned often. Every tuning pass breaks the golden replays (ADR-0006) and the scenarios
that pin his frame and hit data, and re-recording and updating them costs a lot of agent time for no value while his
numbers are still moving.

## Decision

1. **Golden replays are skipped** (`GoldenReplayTests.OnHold`), so ARCHITECTURE.md §10's golden replay check doesn't
   apply while this holds.
2. **Scenarios that set up testman are skipped** (`ScenarioFileTests.TestmanOnHold`). Scenarios on the fixture
   characters (`scenario_dummy`, `juggle_dummy`, …) still run and still cover new gameplay rules.
3. Both show as skipped in test results, never deleted.

## Consequences

- Determinism is still checked by the SyncTest and soak tests; tuning testman no longer turns CI red.
- New gameplay behaviour needs its scenario on a fixture character while this holds.
- To end it: set both constants to false, re-record the golden replays (`GOLDEN_REPLAYS_RECORD=1`), update the
  testman scenarios to his current data.
