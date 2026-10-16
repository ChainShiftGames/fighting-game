# ADR-0015: Shipping game data in exported builds

- **Status:** Proposed (Oct 2026). Amends ADR-0007 (Godot compatibility) and ARCHITECTURE.md §7 (export presets).

## Context

ARCHITECTURE.md §7 and ADR-0007 say the Godot export presets include `data/**/*.json`. That can't work as written:
`data/` sits at the repo root next to `game/` (ARCHITECTURE.md §2), and Godot only exports files inside `res://`
(`game/`). An include filter never reaches `../data`. The data must still ship in every export, match the code it
was built with, and produce the same data hash as in development. Found in P1-07.

Options:
- **A. Embed in `Sim.Data.dll`.** The build embeds `data/**/*.json` as assembly resources; exports read them back.
- **B. Copy into `game/data/` before export** (gitignored copy) + preset include filter; the game reads `res://data`
  through Godot's file API.
- **C. Loose `data/` folder next to the executable**, copied after export; read with `System.IO`.

## Decision

1. **Exported builds read game data embedded in `Sim.Data.dll` (option A).** `Sim.Data.csproj` embeds
   `data/**/*.json` (except `data/schema/`, which is an editor aid) under its data-relative path;
   `EmbeddedDataSource` serves them through the same `IDataSource` interface as the folder on disk.
2. **The editor, tests and tools read the `data/` folder** (`DirectoryDataSource`), so the character editor's live
   reload (ADR-0007) keeps working without rebuilding.
3. Choosing the source is `game/` startup code (when it first loads data): the folder when running from the editor,
   the embedded copy in exports. The export presets don't need a data filter.
4. A test checks that the embedded files are exactly the files in `data/`, byte for byte, and hash the same.
5. No new settings and no new dependencies.

## Consequences

- An export can't ship without its data or with data from another commit; nothing to add to the presets, the
  export steps or the nightly job. Same mechanism on every platform (Windows, Linux, Steam Deck).
- Shipped data can't be edited by players. Fine: data changes would fail the online data hash check anyway, and
  modding is not planned.
- Changing data in a shipped build means rebuilding `Sim.Data`. `dotnet build` and Godot's export already do that.
- B was rejected: it needs a copy step before every export (locally and in CI), and a second copy of the data in
  `game/` that someone will edit by mistake. C was rejected: it needs a post-export copy step and Steam depot setup,
  and it makes the data editable next to the executable.
- ARCHITECTURE.md §7's export-preset line and ADR-0007's Godot note now point here.
