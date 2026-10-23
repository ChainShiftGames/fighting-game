# UI-11 Button icons (glyphs)

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md) (§11: dependencies and asset licences), then
[README.md](README.md) in this folder.
**Where:** `game/UI/Glyphs/` (new), `game/Assets/UI/Glyphs/` (icon images), `tests/FightingGame.Tests/UI/`.
Small changes to screens that show button hints.
**Depends on:** UI-01 (theme, hint bar), UI-02 (device identity and name). Better after UI-10 (bindings to show).

## Goal

Every on-screen hint shows the right button for the device the player is using: Xbox, PlayStation, Switch,
Steam Deck, keyboard keys and a generic set for leverless/arcade controllers. It's needed for Steam Deck and makes
every menu easier to read.

## What to build

- **Device kind detection** from the controller name/GUID Godot reports (and Steam Input's info where present):
  Xbox, PlayStation, Nintendo, Steam Deck, generic. Unknown pads use the generic set.
- **Icon sets** for each kind plus keyboard keys. Use a free asset pack whose licence allows commercial use (Kenney's
  input prompts are CC0); record the source and licence in an ADR, as ARCHITECTURE.md §11 asks for new dependencies.
- **Glyph lookup:** "which icon for menu action *confirm* on this device" and "which icon for game input *C* for this
  player", following the player's real bindings (UI-10).
- **Hint bar** at the bottom of every menu screen: "Ⓐ Select  Ⓑ Back". It follows the **last device used**: switching
  from keyboard to pad updates it right away. On screens with two players (side select, character select) each
  side shows its own player's icons.
- Use the icons in: hint bars, side select device icons (per brand), command list (UI-08) where the move uses a button,
  rebinding screen (UI-10).

## Design notes

- Detection and lookup are plain C#, tested with real controller names (Xbox Wireless Controller, DualSense Wireless
  Controller, Nintendo Switch Pro Controller, Steam Deck, an 8BitDo, an unknown name).
- Icons are presentation assets; nothing in logic or data depends on them.

## Done when

- Plugging a PlayStation pad shows PlayStation icons in hints; switching to the keyboard shows keys.
- Side select shows per-brand icons. Command list shows the player's bound buttons.
- Build, tests and format pass. Screenshots with two different pads.

## Out of scope

Steam Input action sets and Steam's own glyph API (a later Steam task may swap it in behind the same lookup).
