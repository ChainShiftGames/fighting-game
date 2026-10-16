# ADR-0009: Architecture rules are checked by tests

- **Status:** Accepted (Oct 2026)

## Context

ARCHITECTURE.md §2 and §5 require one-way dependencies, an engine-free `Sim`, and mechanics as modules that the
core and other modules never reference. AI agents tend to break these rules quietly (one extra `using`, a shortcut
between modules). Review alone won't reliably catch it.

## Decision

1. Add a `tests/Architecture.Tests` project using **ArchUnitNET** (Apache-2.0, free for commercial use).
2. It runs with the normal tests on every PR and checks:
   - compiled assembly references match the runtime dependency graph in ARCHITECTURE.md §2 (build-time analyzer
     references are not runtime dependencies and are not checked);
   - `Sim` reads gameplay settings only from `MatchRules` (ADR-0011);
   - `Sim` and `Netcode` have no Godot references;
   - the `Sim` core doesn't reference any module namespace;
   - modules don't reference each other;
   - in `game/`, only the match startups (composition roots) name a concrete match driver (ADR-0005 §2);
   - only `Netcode` references Backdash; transports implement Netcode's `ITransport` (ADR-0005 §2);
   - interface names start with `I`.
3. A failing architecture test is fixed by changing the code, not by loosening the test. Changing a rule needs an
   ADR.

## Consequences

- One extra test dependency.
- Module namespaces must follow a fixed pattern (e.g. `Sim.Modules.<Name>`) so the tests can find them.
