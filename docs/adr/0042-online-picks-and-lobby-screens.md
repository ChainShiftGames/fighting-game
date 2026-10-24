# ADR-0042: Online picks travel as lobby member data; online route and lobby screens

- **Status:** Proposed (Oct 2026)

## Context

UI-14 ([brief](../tasks/menus/14-online-screens.md)): online players pick characters on their own PC, and both PCs
must build the identical `MatchLineup` from the same two picks. Until now online matches always played the default
lineup. The lobby (`LobbyFlow`, ADR-0014) already syncs ready marks as Steam lobby member data. The version handshake
(`Netcode/Handshake`) runs inside the match's transport, after the lineup is built.

## Decision

1. **Picks are Steam lobby member data**, like the ready marks: key `pick_for`, value `<match number>:<pick>`.
   `LobbyFlow` treats `<pick>` as opaque text; `game/Online/OnlinePickText` writes and reads it
   (`<colour>/<character id>`). No netcode wire format, `MatchIdentity` or handshake change: Netcode stays free of UI.
   The direct UDP dev path has no lobby, so it still plays the default lineup.
2. **Lobby stages:** both ready in the lobby → `Picking` (character select) → both picks for this match number →
   `Starting`. A pick can't be taken back (as ready can't, ADR-0014's lobby rules); leaving the lobby is the way out.
3. **Rematch keeps the picks:** ready for a rematch re-marks this PC's pick for the new match number, so the rematch
   starts from the results screen without character select or the lobby.
4. **Same lineup on both PCs:** P1 is the lobby owner's pick, P2 the guest's, on both PCs. Colour clashes in a mirror
   follow UI-05's rule when the other pick already arrived (the second to lock in gives way). Two picks locked in the
   same instant can keep the same colour: colours are presentation only, so both PCs still build the same match.
5. **Stage hook:** no stage select yet (UI-06). When there is one, **the host's stage is played**: it travels in the
   host's pick text, which gets a stage field then. Until then every online match uses the default stage.
6. **Online route:** Online (menu + lobby) → Character select (one local side, the remote side shows "waiting" until
   its pick arrives) → Match → Results (`ResultsScreen`, ADR-0044: rematch waits for both, leave the lobby, main menu).
7. **Ping before the match** is Steam's estimate from each member's published ping location (member data
   `ping_location`, `SteamNetworkingUtils`), shown with `PingReading`'s bars. No setting.
8. **Failed pre-match checks** are reported as reasons (`HandshakeRefusal` flags beside the existing refusal text) so
   screens say them through translation keys; a refused match offers no rematch. Never hidden (ARCHITECTURE.md §8).

## Consequences

- No new settings. Picks are visible to anyone who can read the lobby's member data (friends-only lobby).
- A pick naming a character this build doesn't have fails the match start with a message (it can only happen
  between different builds, which the handshake refuses anyway).
- Stage select (UI-06) adds the host's stage to the pick text; results (UI-07, ADR-0044) replaced `MatchOverScreen` online too.
- Steam avatars, join by code and quick match are not built.
