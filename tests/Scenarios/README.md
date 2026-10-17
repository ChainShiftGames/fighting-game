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
  per frame (`2, 3, 6c`), read by the motion parser with the motion window and button buffer from
  `data/input/motions.json`.
- **Units**: positions in subpixels (1 px = 1000), velocities in subpixels per frame, times in frames.
- **Characters**: any character in `data/characters/` or `Fixtures/data/characters/`, loaded together with the
  real button set. `testman` (`data/characters/testman/`, P1-17) is the game's character; its scenarios are in
  `Testman/` and follow its tuning. `scenario_dummy` is the stable fixture for rule scenarios: attacks `5a` (mid), `2a` (low), `6b`
  (overhead), `236b` (mid with chip damage); projectile specials (P1-21; each spawns on move frame 13, 40000 in
  front) `236c` (mid fireball, 8000 per frame, durability 1, projectile 0), `236d` (low fireball at the floor, same
  speed, projectile 1), `214c` (slow mid fireball, 2000 per frame, lifetime 200, durability 2, projectile 2); basic
  moves `idle`, `crouch`, `dash`, `jump`, `landing`, `hitstun`,
  `crouch_hitstun`, `blockstun`, `crouch_blockstun`, `knockdown` (40 frames, no hurtbox), `thrown`, `throw_tech`;
  throws (P1-23) `forward_throw` (5 or 6 + `a+b`) and `back_throw` (4 + `a+b`), active on frames 5-7, reaching 70000
  apart, landing 75000 in front or behind. Frame and hit data are in its move files.
- **Start state**: round 1 as `Sim.Match.RoundStart` sets it up: fighters at x = ∓`roundStartDistance` from
  `data/rules/match.json` (150000), y = 0, facing each other, full health, in idle, timer `roundTimerFrames` (5940).
  Settable values below override it; a later round starts from data again (full health), not from setup.
- **What runs**: the production match, `Sim.Composition.MatchSimulation`: every stage built so far (input step,
  move state machines with the motion parser and button buffer, movement, pushboxes and walls at
  ±`wallDistance` (600000), the camera holding the fighters within `maxPlayerDistance` (640000), hit detection and
  hit resolution with stand/crouch block, projectiles in the entity pool (`entitiesPerPlayer` (4) slots each),
  and the round and timer step: KO, time-out, the `roundOverFrames` (120)
  pause with neutral input, the next round or match over). Match rules come from
  data (`GameData.CreateMatchRules`) plus the setup lines below.
- **Modules** (match rules): `setup: module <name> [config integers…]` enables a registered module; modules take
  lineup slots in the order listed. Config integers are the module's raw config slice. Only these modules run: the
  module lineup in `data/rules/modules*` (switched on and off during development) never reaches a scenario.
- **Air block** (P1-24): `setup: module air_block [mask]`; the mask lists attack kinds that can't be air-blocked
  (`Sim.Modules.AirBlock.AirBlockAttacks`: 1 ground normals, 2 air normals, 4 specials, 8 supers, 16
  projectiles); none means everything blockable can be. Without the module airborne fighters never block.
  Scenario dummy basic move `air_blockstun`.
- **Instant block** (P3-16): `setup: module instant_block <window> <lockout> <blockstun reduction>`
  (`Sim.Modules.InstantBlock.InstantBlockConfig`); an instant block shows as `p<n> event module` on the defender.
- **Throw against strike** (match rules): `setup: throw_vs_strike <rule>` overrides `throwVsStrike` from
  `data/rules/match.json`: `throw` (the throw grabs) or `strike` (the strike hits) when both connect on one frame.
- **Trade rule** (match rules): `setup: trade <rule>` overrides `tradeRule` from `data/rules/match.json` for
  same-frame hits: `trade` (both land) or `clash` (neither lands).
- **Entity pool** (match rules): `setup: entities_per_player <n>` overrides `entitiesPerPlayer` from
  `data/rules/match.json`: projectiles each player can have out at once (0 to 8).
- **Module spawns**: the contract probe's config is `<hitstop> <spawn frame> <owner> <projectile>`; on that frame it
  requests the owner's projectile (index in the character's projectile list, as above), e.g.
  `setup: module contract_probe 0 3 1 0` spawns p2's `236c` fireball at the end of frame 3.

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
| `wins` | fighter | rounds won so far | yes |
| `event <kind>` | fighter | frame events of that kind this frame (`hit`, `block`, `swing`, `landing`, `clash`, `knock_out`, `projectile_spawn`, `projectile_hit`, `projectile_block`, `projectile_clash`, `projectile_expire`, `counter_hit`, `punish_counter`, `throw_grab`, `throw_tech`, `throw_hit`, `module`; projectile and counter hit events belong to the owner or attacker, throw events to the thrower, a tech to each fighter); alone means at least one, or compare a count: `p2 event hit = 0` | no |
| `entities` | fighter | projectiles the fighter has out in the entity pool | no |
| `entity_x` | fighter | x of the fighter's oldest projectile still out; `none` without one | no |
| `entity_durability` | fighter | clash durability left on that projectile; `none` without one | no |
| `camera` | match | logic camera x, the centre of the screen (0 = stage centre) | yes |
| `freeze` | match | global freeze frames left | yes |
| `timer` | match | round timer frames left | yes |
| `round` | match | current round | yes |
| `round_phase` | match | `fighting`, `round_over` (pause after a KO or time-out), `match_over` (= and != only) | no |
| `round_winner` | match | latest round: `p1`, `p2`, `draw`; `none` while fought (= and != only) | no |
| `match_winner` | match | `p1`, `p2`, `draw` once the match is over; `none` until then (= and != only) | no |
| `match_event <kind>` | match | events of no fighter this frame (`time_out`, `round_start`, `match_end`), as `event` | no |
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

The runner itself is in `Runner/`. The match is composed by `src/Sim/Composition/MatchSimulation.cs`, the one
place new gameplay stages plug in, for scenarios, the game and SyncTest alike.
