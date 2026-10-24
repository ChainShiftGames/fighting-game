# UI-07 Results screen and rematch

> **Done:** merged in [PR 107](https://github.com/ChainShiftGames/fighting-game/pull/107) on 2026-10-24.
> Decisions: [ADR-0045](../../adr/0045-results-and-rematch.md) (a player with no controller counts as in for rematch;
> session score held by the results screen, reset at title / main menu / online screens; no "Save replay", every
> match is saved; no stage entry until UI-06).
> Checked by hand: versus to the end with keyboard + pad, both rematching, score kept, character select keeping
> picks, score reset after the main menu. Not yet tried by hand: "Watch again" after a replay, online rematch (two
> PCs). Known risk: online, the result is read from the newest, possibly predicted state; a late rollback that
> changes the winner wouldn't correct the session score.

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md), then [README.md](README.md) in this folder.
**Where:** `game/UI/Results/` (new), replaces the "match over" screen ported in UI-01, `tests/FightingGame.Tests/UI/`.
**Depends on:** UI-01, UI-04 (flow). Better after UI-05 (character select to go back to).

## Goal

A clear end of match: who won, then the next game in as few presses as possible. In a fighting game, getting to the
next game fast matters more than anything else on this screen.

## What to build

- **Result:** winner's character in a win pose or placeholder, "P1 WINS" / "P2 WINS" / draw, rounds won, and
  optional small stats (longest combo, damage) **only if** they can be read from the events/state the match already
  produces. Don't add logic just for stats.
- **Choices:** Rematch · Character select · Stage select · Main menu (plus "Save replay" if the match was recorded and
  not saved automatically; check how `MatchRecording` / `ReplayFolder` work).
- **Rematch needs both players** in versus: each presses rematch; a small tick shows who's in. If one picks something
  else, both go there. Training has no results screen.
- **Win counter** for the session ("P1 3 – 1 P2") that rematches keep and character select keeps. Leaving to the main
  menu resets it.
- Keep the **input grace** (mashing at the end of a fight must not choose anything).
- Online: the rematch handshake belongs to UI-14. Build the screen so that it can wait for the remote player's choice
  (show "Waiting for opponent…").

## Design notes

- Result and rematch state (who agreed, session score) are plain C#, tested without the engine.
- Results never change game state; they read the finished match.

## Done when

- Versus ends → results → both press rematch → new match with the same setup, score updated.
- Character select / stage select / main menu go to the right place and keep or reset the score as described.
- Build, tests and format pass. Screenshot.

## Out of scope

Online rematch handshake (UI-14), detailed match statistics.
