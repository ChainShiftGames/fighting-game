# ADR-0040: Game flow: one plain-C# path from title to match and back

- **Status:** Proposed (Oct 2026)

## Context

UI-04 ([brief](../tasks/menus/04-game-flow-and-main-menu.md)): `MenuScreenChoice` guessed the screen from launch
flags, and `MatchLauncher` picked characters itself. Later menu tasks (side, character and stage select, results,
online screens, settings) each need one place to plug in, and back and "leave match" need one rule.

## Decision

1. **`GameFlow`** (`game/UI/Flow/`, plain C#, tested in `tests/FightingGame.Tests/UI/Flow/`) only decides *which
   screen is next* (`FlowStep`) and the stack below it (`Path`). Screens do their own work and call it (`Choose`,
   `Open`, `Advance`, `Back`, `Rematch`, `Reselect`, `LeaveMatch`, `ToMainMenu`). It never draws or reads devices.
2. **Matches start and stop only through `IMatchStarter`** (`game/Online/`, implemented by `MatchLauncher`):
   `Start(setup)`, `OpenOnline`, `Rematch`, `Stop`. `Follow(MatchPhase)` each frame picks up matches that start or
   end without a menu (lobby, restart, failed start, command line, a Steam invite via `OnlineJoined`).
3. **Routes:** Versus/Training → side select → character select → stage select → match; Online → online screens →
   match. **A step without a screen is skipped**, so the flow works at every stage of the menu tasks.
4. **`FlowScreens` is the single place screens register**: one `Add(step, screen)` line in `GameMenus.Bind`.
5. **`MatchSetup`** (mode + one `FighterPick` per player) is filled by the screens and handed to
   `MatchLauncher.Start`; a rematch plays it again. **Devices are not in it**: they go through `PlayerDevicesChoice`
   ([ADR-0038](0038-device-roster-and-assignment.md)). **Stage joins `MatchSetup` with UI-06.**
6. **Back** walks the route backwards to the main menu, then to the title. **Leaving a match goes back to where its
   mode was picked**: the main menu for local modes, the online screens online. Results/match over offer rematch,
   character select (once it exists) and main menu.
7. **Main menu**: Versus, Training, Online, Replays, Options, Quit; cursor starts on the last mode played this
   session; only Quit asks for confirmation (cursor on Cancel). The title's press sets `LeadDevice`.
8. **Item descriptions** (`MenuItem.Description`) are drawn under every item that has one (`MenuRow`, theme
   variation `MenuItemDescription`), not only the focused one. `IMenuScreen.FirstFocus` sets the cursor on push.

No new settings.

## Consequences

- Each later screen task adds its `IMenuScreen` and one `Add` line; Replays and Options are dimmed until theirs exist
  (UI-09, a replays task).
- `MenuScreenChoice` is gone; command-line launches set `IMatchStarter.Mode` and the flow opens on the match.
