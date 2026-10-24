# UI-12 Accessibility options

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), [ADR-0008](../../adr/0008-presentation-direction.md)
(accessibility, directional audio), then [README.md](README.md) in this folder.
**Where:** `game/Settings/` (new player settings), `game/UI/Settings/Accessibility/`, the presentation code each
option touches (`game/Presentation/`, HUD), `tests/FightingGame.Tests/`.
**Depends on:** UI-01, UI-09 (settings menu with an Accessibility tab).

## Goal

The accessibility options already decided in ADR-0008, as player settings. They change only how the game looks and
sounds, never logic results.

## Options

- **Screen shake:** off / reduced / full.
- **Flashes:** reduce full-screen flashes and strong hit flashes (photosensitivity). Effects already go through
  toggles (ADR-0008); find where shake and flash are triggered in `game/Presentation/Feel/` and `Events/`.
- **Colour-blind-safe colours:** a palette for P1/P2 colours, HUD gauges and menu highlights that works for the common
  types (deuteranopia, protanopia, tritanopia). One option with presets, not per-colour pickers.
- **Text size:** scales all menu and HUD text (builds on UI-09's UI scale; make sure layouts don't break at the
  largest size, including on 1280×800).
- **Input display:** always-on input history in matches (not only training), per side.
- **Directional audio (ADR-0008):** sounds panned by on-screen position, distinct cues for important events (block,
  hit, throw, wall). Build the pan-by-position part and a setting for its strength; list which cues still need
  distinct sounds, don't author audio.
- **Hold instead of mash** for menu confirms where mashing is common (optional, simple toggle).

## Design notes

- Each option is a player setting with its own small applier; adding one doesn't edit the others.
- Developer visual toggles stay developer settings. These player options sit on top (the player can reduce, never
  re-enable something the devs turned off).
- Tests: setting values, colour palette choice, pan calculation from position (plain C#).

## Done when

- Every option above changes the game as described, is saved, and survives a restart.
- Largest text size has no clipped text on 1280×800 (screenshots).
- Build, tests and format pass.

## Out of scope

New sound effects, screen reader support for menus (note it as a follow-up if you see an easy path).
