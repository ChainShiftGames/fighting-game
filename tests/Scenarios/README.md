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
  (overhead), `236b` (mid with chip damage; `5special+b`); projectile specials (P1-21; each spawns on move frame 13, 40000 in
  front) `236c` (mid fireball, 8000 per frame, durability 1, projectile 0), `236d` (low fireball at the floor, same
  speed, projectile 1), `214c` (slow mid fireball, 2000 per frame, lifetime 200, durability 2, projectile 2); basic
  moves `idle`, `crouch`, `dash`, `jump`, `landing`, `hitstun`,
  `crouch_hitstun`, `blockstun`, `crouch_blockstun`, `knockdown` (40 frames, no hurtbox), `thrown`, `throw_tech`;
  throws (P1-23) `forward_throw` (5 or 6 + `a+b`) and `back_throw` (4 + `a+b`), active on frames 5-7, reaching 70000
  apart, landing 75000 in front or behind. Frame and hit data are in its move files.
  `juggle_dummy` (hit data of ADR-0022, all hits without hitstop or pushback, tall hit boxes 20000-70000 in front):
  `5a` two hits (frames 5-6: 300, 9-10: 400), `2a` launcher (startup 5, launch x 500 y 20000, 30 hitstun), `5b`
  air juggle hit (startup 3, airLaunch x 1000 y 12000), `5c` (startup 5, launch x 6000 y 10000, ground bounce
  y 15000, wall bounce x 8000 y 8000, knockdownFrames 60); its `air_hitstun` move has a hurtbox.
  `sparring_dummy`'s `2a` is an OTG hit (its knockdown has a hurtbox), its `5a` isn't.
- **Start state**: round 1 as `Sim.Match.RoundStart` sets it up: fighters at x = ∓`roundStartDistance` from
  `data/rules/match.json` (150000), y = 0, facing each other, full health, in idle, timer `roundTimerFrames` (5940).
  Settable values below override it; a later round starts from data again (full health), not from setup.
- **What runs**: the production match, `Sim.Composition.MatchSimulation`: every stage built so far (input step,
  move state machines with the motion parser and button buffer, movement, pushboxes and walls at
  ±`wallDistance` (600000), the camera holding the fighters within `maxPlayerDistance` (380000), hit detection and
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
- **Combo limits** (P3-12): `setup: module combo_limits <damage proration‰> <minimum damage‰> <hitstun decay>
  <maximum decay> <minimum hitstun> <gravity scaling‰> <maximum gravity‰> <OTG hits>`
  (`Sim.Modules.ComboLimits.ComboLimitsConfig`). Its scenarios use `sparring_dummy`
  (`Fixtures/data/characters/sparring_dummy/`): `5a` and `2a` (1000 damage, 20 hitstun, startup 3; `2a` knocks
  down), and a `knockdown` that keeps a low hurtbox so OTG hits can connect.
- **Move conditions** (ADR-0023): `mechanics_dummy` (`Fixtures/data/characters/mechanics_dummy/`) has a rekka on
  236a (`rekka_1` → `rekka_2` → `rekka_3`, follow-up windows on frames 8-21), a stance (214b `stance_enter` sets
  it; in it 5c is `stance_c`, which leaves it; outside it the plain `5c`), and an install: 214d `install` costs 5000
  of the main meter (needs `module meter`) and starts its `install` mechanic (30 frames, slice `[0]` = frames left),
  during which 6c is `install_c`. Its `5b` hits on its first frame (6 hitstop) and has a follow-up into `5b_2`
  (5 + b) from its frame 2 on hit or block, for input consumption (ADR-0033).
- **Character mechanics** (ADR-0023): a character's data switches its mechanic on, so no setup line; read its slice
  with `p1 mechanic <i>`. `gauge_dummy` (`Fixtures/data/characters/gauge_dummy/`) runs `character_gauge` on channel
  1: starts at 100, +1 per frame, +50 per hit and +20 per block of its own `5a` (scenario_dummy's).
- **Meter** (P3-13): `setup: module meter <superCost> <hitGain> <blockGain> <forwardGain> <roundStartMeter>
  <carryOverRounds> [channel]` (hundredths of a percent; channel 0 when left out); read with `module meter[0]` (P1) /
  `[1]` (P2).
- **Burst** (P3-17): `setup: module burst <maxGauge> <startGauge> <gainPerFrame> <gainWhenHit> <burstCost>
  <usableInHitstun> <usableInBlockstun> <usableInNeutral> <triggerButtons> <cancelPriority>`
  (`Sim.Modules.Burst.BurstConfig`; triggers `240` = a+b+c+d). The burst cancels into the character's `burst` move
  role (`moveRoles` in its `character.json`), so a character without one can't play with the module on. Slice:
  `module burst[0]` P1 gauge, `[1]` P1 bursts used, `[2]` P2 gauge, `[3]` P2 bursts used.
- **Negative penalty** (P3-21): `setup: module negative_penalty <penaltyFrames> <forwardRecovery> <meterLoss>`
  (`Sim.Modules.NegativePenalty.NegativePenaltyConfig`); the retreat counters are `module negative_penalty[0]` /
  `[1]`, a penalty shows as `p<n> event module`.
- **Throw against strike** (match rules): `setup: throw_vs_strike <rule>` overrides `throwVsStrike` from
  `data/rules/match.json`: `throw` (the throw grabs) or `strike` (the strike hits) when both connect on one frame.
- **Trade rule** (match rules): `setup: trade <rule>` overrides `tradeRule` from `data/rules/match.json` for
  same-frame hits: `trade` (both land) or `clash` (neither lands).
- **Pushback guard** (P3-15): `setup: module pushback_guard <buttonMask> <buttonCount> <pushbackPermille> <meterCost>
  <drainPerFrame> <noChip>` (`Sim.Modules.PushbackGuard.PushbackGuardConfig`; `48` = a+b). Hold back + the buttons (`4a+b`):
  a guarded block shows as `p<n> event module` on the defender; with a cost it needs `module meter`.
- **Roman Cancel** (P3-18): `setup: module roman_cancel <buttonMask> <buttonCount> <pressWindow> <meterCost>
  <variants> <freezeFrames> <slowdownFrames> <slowdownNumerator> <slowdownDenominator>`
  (`Sim.Modules.RomanCancel.RomanCancelConfig`; button bits a 16, b 32, c 64, d 128: `112` = a+b+c; variants 1 red,
  2 purple, 4 blue, 8 yellow). A Roman Cancel shows as `p<n> event module`; with a cost it needs `module meter`.
- **Special button shortcut** (ADR-0020): `data/input/motions.json` maps 236 → `5`, 623 → `6`, 214 → `4`; that
  direction + `special` + the move's button starts the move (`5special+c` gives 236c, `6special+c` 623c). A move from
  a multi-step motion wins over it; it wins over single-direction moves (5c). `setup: shortcut_damage <percent>` and
  `setup: shortcut_cooldown <frames>` override `shortcutDamagePercent` and `shortcutCooldownFrames` from
  `data/rules/match.json` (100, 0: off).
- **Air recovery** (match rule, ADR-0022): `setup: air_recovery <0|1>` overrides `airRecovery` from
  `data/rules/match.json` (off): on, hitstun ending in the air frees the fighter into a jump.
- **Pushback**: a hit or block slides the defender its hit's `pushback` over `pushbackFrames` (8, from
  `data/rules/match.json`) once its hitstop ends, in equal shares; against a wall the rest pushes the attacker back
  (never for projectiles). Scenarios that check x after a hit see the slide.
- **Entity pool** (match rules): `setup: entities_per_player <n>` overrides `entitiesPerPlayer` from
  `data/rules/match.json`: projectiles each player can have out at once (0 to 8).
- **Screen width** (match rules): `setup: max_player_distance <subpixels>` overrides `maxPlayerDistance` from
  `data/rules/match.json`, for scenarios that need the fighters further apart than the shipped screen allows.
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
| `mechanic <i>` | fighter | integer i of the state slice of the fighter's character mechanic (ADR-0023) | no |
| `flags` | fighter | the fighter's flags, bit i = flag i of `data/rules/flags.json` (1 stance, 2 install) | yes |

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
