# UI-06 Stage select and versus screen

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), [ADR-0030](../../adr/0030-stage-art-pipeline.md)
(stages), then [README.md](README.md) in this folder.
**Where:** `game/UI/StageSelect/`, `game/UI/Versus/` (new), the stage loading in the match view if it needs a stage id,
`tests/FightingGame.Tests/UI/`.
**Depends on:** UI-01, UI-04 (flow and match setup), UI-05 (characters chosen before this).

## Goal

After character select: pick a stage, then a short versus splash before the fight. First check how stages work today
(`game/Assets/Stages/`, ADR-0030): whether stages are pure presentation or affect the logic (walls, stage width
come from match rules). If a stage choice would change logic results, stop and raise it, because then it has to be
part of what both online players agree on.

## Stage select

- List or carousel of the stages found in `game/Assets/Stages/` (skip `_template`), with a preview image and name.
  A new stage folder appears without code changes.
- **Random** stage option. Remembers the last stage this session.
- Who picks: in versus, either player can move the cursor; the first confirm wins. (Online: UI-14 decides; the
  default is the host or a random pick.)
- If there's only one stage, skip the screen.

## Versus screen

- Both characters, names and colours facing each other, "VS", the stage name. About 2 seconds (developer
  setting), and any player can press confirm to skip it.
- Use the time to load the match: the match should be ready when the splash ends, so it never adds waiting time.
- Optional, behind a developer toggle: a short intro line per character.

## Design notes

- Stage list and selection logic in plain C#, tested without the engine.
- The chosen stage id goes into the match setup and, if it's presentation only, into the replay header only if the
  format already has room (don't change the replay format without an ADR).

## Done when

- Character select → stage select → versus → match, with the chosen stage on screen.
- Skipping works, one-stage skip works, random works.
- Build, tests and format pass. Screenshots of both screens.

## Out of scope

New stage art, stage music (later), online stage agreement (UI-14).
