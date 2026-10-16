# Scenario tests

Plain-text gameplay tests (ADR-0006, ARCHITECTURE.md §10). Each `*.scenario` file under this folder is one test
case, named by its path; `dotnet test` runs them all. **Adding a scenario needs no C#**: write the file, run the
tests. Files are read from the source tree, so `dotnet test --no-build` sees edits too.

Put files in `tests/Scenarios/<Area or Module>/` (CONVENTIONS.md), named in `snake_case` after what they prove.

## Format

One statement per line; `#` starts a comment; case is ignored. Setup lines come first.

```
# Hitstop runs down one frame at a time.
setup: p1 character scenario_dummy        # both characters are required
setup: p2 character scenario_dummy
setup: p2 hitstop = 3                     # change the start state

frame 1: p1 6a                            # input on one frame
frames 2-10: p1 2                         # held over a range (inclusive)
frame 11: p1 2, 3, 6c                     # sequence: frames 11, 12, 13

frame 1: expect p2 hitstop = 2            # checked after frame 1
frames 3-10: expect p2 hitstop = 0        # checked after every frame of the range
frame 2: expect p1 x < 0                  # = != < <= > >=
```

- **Frames** are 1-based logic frames: frame N's input is read on the Nth tick and its expectations are checked after
  it (same counting as frame data: a startup-5 move pressed on frame 1 is active on frame 5). The run ends after
  the last frame that has an input or expectation.
- **Inputs** are numpad notation, **relative to the fighter's facing** at the start of the frame (6 = forward):
  a direction digit and button ids from `data/input/buttons.json` (`a b c d special dash`), joined with `+`: `5`,
  `6a`, `2c+d`, `6dash`, `4+6` (raw left+right, before SOCD). No input means neutral. A motion is one direction
  per frame (`2, 3, 6c`); until the motion parser (P1-06) moves only trigger on a button press plus one direction.
- **Units**: positions in subpixels (1 px = 1000), velocities in subpixels per frame, times in frames.
- **Characters**: any character in `data/characters/` or `Fixtures/data/characters/`, loaded together with the
  real button set. `scenario_dummy` (moves `5a`, `idle`, `crouch`) is there until Testman (P1-17).
- **Start state** (until round start, P1-13): fighters at x = ∓150000, y = 0, facing each other, full health, in
  idle, round 1. Settable values below override it.
- **What runs**: every stage built so far (input step, move state machines); the others still do nothing, so e.g.
  nothing moves or gets hit yet. `Runner/ScenarioMatch.cs` lists them.
- **Modules** (match rules): `setup: module <name> [config integers…]` enables a registered module; modules take
  lineup slots in the order listed. Config integers are the module's raw config slice.

## Expectations

`frame N: expect [p1|p2] <keyword> <comparison> <value>`. A fighter keyword needs `p1`/`p2`; a match keyword has
none. Settable ones also work in setup: `setup: [p1|p2] <keyword> = <value>`.

| Keyword | Of | Value | Settable |
|---|---|---|---|
| `x`, `y` | fighter | position (y up from the floor) | yes |
| `vx`, `vy` | fighter | velocity | yes |
| `facing` | fighter | `left` / `right` (= and != only) | yes |
| `health` | fighter | health left | yes |
| `damage` | fighter | character's stats.health − health | no |
| `hitstop` | fighter | hitstop frames left | yes |
| `input` | fighter | input recorded this frame, after SOCD, as notation (= and != only) | no |
| `state` | fighter | `idle`, `crouch`, `walk_forward`, `hitstun`, `attack`… (= and != only) | no |
| `move` | fighter | id of the playing move, e.g. `5a` (= and != only) | no |
| `move_frame` | fighter | frame of the playing move, 1 on its first frame | no |
| `stun` | fighter | hitstun or blockstun frames left | no |
| `event <kind>` | fighter | frame events of that kind this frame (`hit`, `block`, `swing`, `landing`); alone means at least one, or compare a count: `p2 event hit = 0` | no |
| `freeze` | match | global freeze frames left | yes |
| `timer` | match | round timer frames left | yes |
| `round` | match | current round | yes |
| `module <name>[i]` | match | integer i of an enabled module's state slice | no |

A failing scenario reports only the **first** failing expectation, as
`file:line: frame N: expected <expectation>, but it was <actual>`, followed by every value above for both fighters
and the match. Unreadable files fail with `file:line: reason`.

## Adding an expectation kind (C#, for gameplay tasks)

One class in `Expectations/Kinds/`, nothing else: kinds are found by reflection, and the parser, the setup and
the failure report pick it up.

- One integer of a fighter or the match: derive from `ValueKind` (`Keyword`, `IsPerFighter`, `Read`), as
  `HitstopKind` does. Named values override `ParseValue`/`FormatValue`, like `StateKind` or `MoveKind`; add
  `ISettableKind` to allow it in setup.
- Anything else (extra arguments, counts): implement `IExpectationKind` and return an `IExpectation`, like
  `EventKind`.
- Add a scenario using it, and a line to the table above.

The runner itself is in `Runner/`; `ScenarioMatch` is the one place that composes the match (pipeline stages,
match rules, start state), so new gameplay stages plug in there.
