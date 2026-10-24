# UI-05 Character select

> **Done:** merged in [PR 100](https://github.com/ChainShiftGames/fighting-game/pull/100) on 2026-10-24.
> Decisions: [ADR-0041](../../adr/0041-character-select.md) (colours and descriptions in
> `game/Assets/Characters/<id>/<id>_select.json`, outside `data/`; no controller → mirror in the next free colour).
> Checked by hand: P1 picking with P2 mirroring (no pads connected). Not yet tried by hand: two cursors on two pads.
> Not built: the fighter isn't tinted in the chosen colour yet (recorded in the replay, not drawn).

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md) (§7 character data), then [README.md](README.md) in this
folder.
**Where:** `game/UI/CharacterSelect/` (new), `game/Online/MatchLineup.cs` (take the chosen characters and colours),
`tests/FightingGame.Tests/UI/CharacterSelect/`.
**Depends on:** UI-01, UI-04 (flow and match setup). Uses UI-03's device assignment so each player drives their own
cursor.

## Goal

Both players pick a character and a colour at the same time, each with their own controller, then confirm. Today
`MatchLineup.From` plays the first character for both sides in colour 0.

## What to build

- **Grid of portraits** built from the loaded characters (`GameData.Characters`), so a new character in `data/`
  appears without code changes. Placeholder portrait: the name on a card until there's art (character art files under
  `game/Assets/Characters/<id>/`).
- **Two cursors at once**, P1 and P2 colours, each moved only by that player's devices. Both can be on the same
  character (mirror match).
- **Preview** per side: large picture/model of the hovered character (the fighter model where one exists; the
  placeholder otherwise), name, and a one-line description of how it plays (if the character data has none yet,
  suggest adding an optional display field and keep it out of the data hash).
- **Colour pick:** after choosing a character, left/right cycles its colours (palettes), then confirm readies. In a
  mirror match, the second player can't take the same colour, or it's shifted to the next one automatically.
- **Random** slot: picks a random character and colour (presentation randomness, never the logic PRNG).
- **Ready and cancel:** back un-readies, then backs out of the colour, then leaves to side select. Once both are
  ready, a short "both ready" moment, then on to stage select. Training: only P1 picks; the dummy picks after, or
  defaults to a mirror (pick one and say why).
- **Remembers** each player's last character and colour for the session, so rematch → character select starts there.
- Output: characters + colours into the match setup (UI-04), used by `MatchLineup` and written into the replay header
  (`ReplayPlayer` already has a colour).

## Design notes

- Selection state (cursors, steps, readiness, mirror colour rule, random) is plain C#, tested without the engine.
- Characters and colours come from data; nothing about a specific character in the screen code.
- Online character select happens per player on their own PC and is exchanged through the online flow (UI-14); keep
  the screen usable with only one local side.

## Done when

- Two players pick at once on two controllers; the match starts with the right characters and colours, and the
  replay records them.
- Mirror match, random, back-out and session memory work.
- Logic tests for the selection rules. Build, tests and format pass. Screenshot with both cursors.

## Out of scope

Real portraits and models, stage select (UI-06), unlocks.
