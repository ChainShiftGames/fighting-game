# ADR-0044: Results screen: per-player choices, rematch agreement, session score

- **Status:** Proposed (Oct 2026)

## Context

UI-07 ([brief](../tasks/menus/07-results-and-rematch.md)) replaces the placeholder `MatchOverScreen` for local
matches, online matches and replays. Rematch in versus needs both players; online already needs both PCs (ADR-0042
§3). A session win counter must survive rematches and character select.

## Decision

1. **Each player has their own cursor** (`ResultsChoices`), like character select (ADR-0041): one list, a P1 marker
   left and a P2 marker right. Rematch ticks that player in (back takes it back); any other entry one player
   confirms takes both there. Drawn by its own `ResultsView`; the screen sets `TakesEveryPress`.
2. **A player without a connected device counts as in** (P2 when only the keyboard is there, who mirrors on character
   select): asked live, so a pad plugged in on the screen has to press too. At least one player must press, so a PC
   with no devices never rematches by itself.
3. **Online:** one chooser (this PC's player, drawn on its lobby side). Rematch marks the lobby ready as before
   (can't be taken back); ticks come from the lobby's ready marks. Replays: one cursor for any device, "Watch again".
4. **Session score** (`SessionScore`, plain C# held by the results screen, never game state): counts each finished
   match except replays; reset when the flow reaches the title, the main menu or the online screens (a new lobby may
   mean a new opponent). A match without a result (desync, refused checks) doesn't count.
5. **The result is read, never computed:** `MatchResult.From(RoundState)` (match winners, rounds won) through
   `StartedMatch.Round`. No stats: nothing the match already produces gives them without new logic.
6. **No stage select or "Save replay" entry:** stage select doesn't exist yet (UI-06 adds its entry and flow step);
   every finished match is already saved as a replay.

## Consequences

- No new settings. The win pose is the match itself, still drawn behind the overlay.
- Online the result is read from the newest state when the results open; a rollback that changed the winner after
  that would not correct the session score (it can't change the saved replay, which waits for confirmed frames).
