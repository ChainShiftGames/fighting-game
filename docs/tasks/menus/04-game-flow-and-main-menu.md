# UI-04 Game flow, title and main menu

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), then [README.md](README.md) in this folder.
**Where:** `game/UI/Flow/`, `game/UI/Title/`, `game/UI/MainMenu/` (new), `game/UI/GameMenus.cs` (shrinks or goes),
`game/Main.cs` (wiring only), `tests/FightingGame.Tests/UI/Flow/`.
**Depends on:** UI-01. Uses UI-03 when it exists (until then, skip side select and use the default assignment).

## Goal

One clear path through the game, from boot to match and back, instead of `MenuScreenChoice` guessing the screen
from launch flags. Plus a real title screen and main menu.

## Flow

```
Boot → Title ("Press any button") → Main menu
Main menu: Versus · Training · Online · Replays · Options · Quit
Versus / Training → Side select (UI-03) → Character select (UI-05) → Stage select + versus screen (UI-06) → Match
Match → Results (UI-07) → Rematch | Character select | Main menu
Online → Online screens (UI-14) → … → Match
Options → Settings (UI-09)
```

- Screens that don't exist yet are skipped (character select → today's default lineup, etc.), so the flow works at
  every step of the work. Each later task plugs its screen in with one line.
- What the player picked so far (mode, devices, characters, colours, stage) travels as one plain *match setup* object
  that the flow fills in and hands to `MatchLauncher` at the end. `MatchLauncher` takes that setup instead of choosing
  characters itself.
- Back walks the flow backwards (character select → side select → main menu). Leaving a match goes to the screen the
  mode came from.
- Command-line launches that skip the menus (direct online, replay files, dev shortcuts) keep working.

## Title and main menu

- **Title:** game logo placeholder, "Press any button". The press also tells the menus which device the player is on.
  Fast: no unskippable intro, boots straight here.
- **Main menu:** the list above. Focus starts on the last mode used this session. A short line under each item
  says what it is. Quit asks for confirmation; nothing else does.
- **Idle demo / attract mode:** not now.

## Design notes

- The flow is a small, plain-C# state machine with no Godot types, tested on its own: forward, back, skip missing
  screens, rematch keeps the setup, leaving online goes back to the online screens.
- Don't let it become a `GameController`: the flow only decides *which screen is next*; screens do their own work.
- Existing behaviour that must survive: rematch, leaving a match from pause, online lobby entry, the input grace after
  a fight.

## Done when

- Boot → title → main menu → versus → match → results/match over → rematch or back, all on a gamepad.
- Training and online are reachable as today. Command-line launch modes still work.
- Flow tests pass. Build, tests and format pass. Screenshots of title and main menu.

## Out of scope

The screens themselves (side select, character, stage, results, settings, online).
