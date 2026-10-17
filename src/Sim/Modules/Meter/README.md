# Meter module (P3-13)

Tension-style meter per fighter, in hundredths of a percent (`IMeterGauge.FullMeter` = 10000 = 100 %).
Config keys (`data/rules/modules/meter.json`, developer settings): `superCost`, `hitGain`, `blockGain`,
`forwardGain` (per frame ending further forward), `roundStartMeter`, `carryOverRounds` (0/1).
State slice: `[0]` P1 meter, `[1]` P2 meter (scenarios: `module meter[0]`).

## Supers

- Meter below `superCost` → the module requests a `MoveLockRequest` on `MoveCategory.Super` before the state
  machines; the input falls through to the next matching move.
- A super started this frame (`FighterState.StartedMoveCategory`) is paid right after the state machines.

## API for other modules (pushback guard, Roman Cancel, …)

Never read this module's slice. Use the core, through `ModuleContext`:

- `context.MeterOf(fighter)`: current meter; 0 when no gauge is enabled.
- `context.TrySpendMeter(fighter, amount)`: spends immediately and returns true if the fighter had `amount`;
  false (nothing spent) without enough meter or without a gauge. Later hooks in the same frame see the new value.

The core routes both to the first enabled module implementing `IMeterGauge` (`Sim.Modules.MeterAccess`).
Run this module early in `modules.json` order so a spend in an earlier hook is reflected in the super lock.
