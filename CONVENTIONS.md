# Conventions

> **Proposal: both devs confirm before merge.** Rules live in [ARCHITECTURE.md](ARCHITECTURE.md); this file
> only adds layout and naming it doesn't cover. Code naming and file-per-type: ARCHITECTURE.md §5.

## Folders inside each project

- **Folder = namespace segment.** `src/Sim/Collision/PushboxSolver.cs` → `namespace Sim.Collision`. Root namespace
  = project name (`Sim`, `Sim.Data`, `Netcode`, `Platform.Steam`, `FightingGame` for `game/`).
- **Group by feature, not by kind.** `Sim/Combat/`, not `Sim/Interfaces/` or `Sim/Structs/`. The folder names in
  a task brief's `Where:` line are the plan; follow them.
- **`src/Sim`:** one folder per tick stage or concept (`Input/`, `Fighters/`, `Collision/`, `Combat/`, `Match/`,
  `State/`, `Rules/`, …).
  - `Sim/Modules/` holds the module contract (namespace `Sim.Modules`).
  - Each mechanic is `Sim/Modules/<Name>/` → `Sim.Modules.<Name>` (ADR-0009). Nothing outside its folder except
    the one registration line.
- **`src/Netcode`:** `Drivers/`, `Transport/`, `Online/`, `Replays/`, `Desync/`, `Handshake/`, `Bots/`.
- **`game/`:** C# feature folders in PascalCase (`Input/`, `Presentation/`, `UI/`, `Training/`, `Debug/`,
  `Diagnostics/`); a scene sits next to its script. Shared raw assets in `game/Assets/<Kind>/` (`Sprites/`, `Audio/`,
  `Fonts/`). Editor plugins in `game/addons/<snake_case>/` (Godot convention).
- **`tests/`:** one project per tested project (`Sim.Tests`, `Sim.Data.Tests`, …), folders mirror the source.
  Scenarios in `tests/Scenarios/<Area or Module>/`.
- **`tools/<ToolName>/`:** one project per tool (`Benchmarks/`, `DesyncDiff/`, `Analyzers/`).

## File and asset naming

- **C#:** `<TypeName>.cs`. Tests: `<TypeUnderTest>Tests.cs`; test methods are PascalCase sentences
  (`PointBlank5AHitsOnFrame5`).
- **Character data:** `data/characters/<character_id>/character.json`, moves in
  `data/characters/<character_id>/moves/<move_id>.json`. Schemas: `data/schema/<kind>.schema.json`.
- **IDs and data file names:** lowercase `snake_case` ASCII (`testman`, `5a`, `236c`, `j_b`). Paths are
  case-sensitive on Linux/Steam Deck, so lowercase avoids "works on Windows" bugs.
- **JSON keys:** `camelCase`.
- **Godot:** scene and its C# script share the type name (`HitSpark.tscn` + `HitSpark.cs`). Raw assets
  (`.png`, `.wav`, `.ogg`, `.ttf`) are `snake_case`, named subject first (`testman_idle_00.png`, `hit_light.wav`).
- **Godot `res://` paths are case-sensitive in exports:** match the file's case exactly.

## Branches

- Task work: `<area>/<nn>-<short-name>`, from the brief's file name (`ui/01-menu-framework`). Task briefs:
  [docs/tasks/README.md](docs/tasks/README.md).
- Other work: `docs/<short-name>` or `fix/<short-name>`.

## Commits

```
UI-01: Add the menu framework           ← task ID + imperative summary, ≤ 72 chars

Why the change was made, wrapped at 72. Mention ADRs, golden replay
or MatchRules changes here.

Co-Authored-By: <agent name> <noreply@…>   ← agent commits only
```

- No task ID for non-task work: just the imperative summary.
- One logical change per commit; PR title = the task's commit summary.
