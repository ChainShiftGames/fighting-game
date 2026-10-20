# Phase 5 — Roster & content (outline)

**Goal:** launch content and all player-facing menus.
**Exit (from ROADMAP):** launch content complete; every character passes the uniqueness check.

> Outline only. Expand into full tasks when Phase 4 is nearly done.

## Decisions to make at phase start
- Final roster (ROADMAP §8 Q7) and which engine feature each character proves.

## Workstreams
- **P5-A Characters 2–4:** each with its own game plan and at least one unique mechanic (as a module if data
  can't express it). Uniqueness check per character (GAP-38).
- **P5-B Stages 2–3 and music.**
- **P5-C Palettes and costumes:** palette textures; costumes as separate models.
- **P5-D Menus:** main menu, character select, versus, results. Placeholder menus exist (`game/UI/GameMenus.cs`: main,
  online lobby, match over, pause); restyle or replace them, and add character select (both sides are Testman now).
- **P5-E Player settings menu and persistence:** keybinds, vsync, input-delay override, audio,
  language, accessibility. Versioned JSON in `user://`, Steam Cloud sync. Only **player settings** appear here.
- **P5-F Rebinding UI** per device.
- **P5-G Localization groundwork:** all text through Godot translation tables, Japanese-capable font, English only.
- **P5-H Accessibility:** shake/flash toggles, colour-blind-safe colours, scalable text, input display,
  directional audio cues.
