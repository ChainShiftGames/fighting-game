# Menus and player experience: task briefs

One brief per file. Each one can be handed to an agent on its own. This README holds the context they all share, so
read it once before any brief.

## What we're building

A 1v1 2D fighting game with rollback netcode in Godot .NET (overview: [ARCHITECTURE.md](../../../ARCHITECTURE.md) §1).
The fighting itself works: local versus, training mode, online over Steam, replays. Its menus are still
placeholders. This set of tasks turns them into what players expect from a modern fighting game: a real menu system
that controllers drive well, choosing a side per controller, character and stage select, results and rematch,
settings with full rebinding, button icons and accessibility.

The player's path through the game, once these tasks are done:

```
Title → Main menu → Mode (Versus / Training / Online / Replays / Options)
      → Side select (controllers slide to P1 / P2) → Character select → Stage select → Versus screen
      → Match (pause menu) → Results → Rematch / Character select / Main menu
```

## Rules every brief adds to ARCHITECTURE.md

- **Presentation only.** Menus live in `game/` and never change game logic. What they choose (characters, colours,
  stage, devices) is handed to the match startup before the match begins.
- **Every setting has an audience** (ARCHITECTURE.md §6). Anything a player changes in a menu is a *player setting*
  and must never change logic results. Match rules are never in player menus.
- **Logic separate from nodes.** Put each screen's state and decisions (navigation, who picked what, readiness) in
  plain C# classes with no Godot types, so `tests/FightingGame.Tests` can test them without the engine (ADR-0016).
  Godot nodes only draw that state and forward input to it.
- **Text through translation keys** once task 01 lands: no hard-coded English in new UI code.
- **Controllers first.** Every screen works fully with a gamepad, a leverless/hitbox controller and the keyboard;
  the mouse is a bonus. Both players can use menus at the same time where a screen needs it.
- **Mashing is safe.** A menu that opens right after a fight ignores presses for a short grace time
  (`MenuTiming.GraceMilliseconds`), so button mashing never chooses something by accident.
- **Placeholder look is fine.** Clean, readable, consistent with the shared theme from task 01. Final art comes later
  and goes through the same scenes.

## Where things are today

- Menus: `game/UI/`, built on the menu framework ([ADR-0039](../../adr/0039-menu-framework.md)): `Framework/`
  (stack, focus, actions, text keys), `Widgets/`, `Screens/`. `GameMenus.cs` keeps the stack on the screen that fits
  (`MenuScreenChoice`) and turns choices into `MatchLauncher` calls (`game/Online/MatchLauncher.cs`). Theme and
  translation table: `game/Assets/UI/`. `OptionsMenu.cs` is a key-driven F12 overlay (vsync, input delay).
- Training menu: `game/Training/TrainingMenu*.cs`, entries implement `ITrainingMenuEntry`.
- Input: `game/Input/`. `JoypadRoster` + `JoypadWatcher` track pads (keep the **no-double-input rule** with Steam
  Input documented in `JoypadRoster`). `DeviceRoster` lists devices (the keyboard + pads) and raises
  connect/disconnect events; match startups read a `PlayerDevices` assignment from `PlayerDevicesChoice`
  ([ADR-0038](../../adr/0038-device-roster-and-assignment.md)). Bindings: `default_bindings.jsonc`, copied to
  `user://dev_bindings.jsonc`.
- Player settings: `game/Settings/` (`PlayerSettings` record, JSON in `user://` via `PlayerSettingsStore`).
- Who plays what: `game/Online/MatchLineup.cs` (currently the first character for both, colour 0).
- HUD font: `HudLettering.FontPath` in `game/Presentation/Hud/`.
- Startup wiring: `game/Main.cs`.

## Order and parallel work

```
01 Menu framework ──┬─> 04 Game flow + main menu ─> 05 Character select ─> 06 Stage + versus ─> 07 Results + rematch
02 Device roster ───┼─> 03 Side select (needs 01 + 02) ──────────┘
                    ├─> 08 Pause menu      ├─> 09 Settings menu ─> 10 Controls + rebinding ─> 11 Button icons
                    ├─> 12 Accessibility (after 09)
                    ├─> 13 Training menus
                    └─> 14 Online screens (after 04)
```

- **01 first.** 02 can run alongside it (it only touches `game/Input/` and the match startups).
- After 01 + 02, 03/04/08/09/13 can run in parallel; they touch different screens.
- Two agents never edit the same files at once. If a brief has to change a shared file (`Main.cs`, `GameMenus.cs`,
  a match startup), keep that change small and mention it in the PR.

## For every task

- Read ARCHITECTURE.md first; its rules override the brief.
- Write a *Proposed* ADR for any lasting decision (next free number, one line in `docs/adr/README.md`).
- Branch and commit naming: [CONVENTIONS.md](../../../CONVENTIONS.md).
- Before the PR: `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` pass, plus the Godot
  editor-side build when `game/` changed (commands in CLAUDE.md).
- If the Godot editor MCP is available, run the game and take screenshots of each new screen to check it.
- Return a short summary of what changed, how you checked it, and anything left unresolved.
