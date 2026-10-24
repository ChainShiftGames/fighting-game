# UI-14 Online screens

> **Done:** merged in [PR 101](https://github.com/ChainShiftGames/fighting-game/pull/101) (online screens) and
> [PR 102](https://github.com/ChainShiftGames/fighting-game/pull/102) (replay browser) on 2026-10-24.
> Decisions: [ADR-0042](../../adr/0042-online-picks-and-lobby-screens.md) (picks as lobby member data; host's stage
> once there is stage select).
> Checked by hand on one PC with Steam running: online menu, hosting a lobby alone, leaving it, the replay browser
> (list, unsupported replays, entry, delete confirmation, watching). Not yet tried by hand: anything with a second
> player (lobby with an opponent and ping, online character select, the match, rematch, disconnects, a refused
> version check); needs two PCs with Steam. Not built: Steam avatars, join by code, the winner in the replay list.

**Read first:** [ARCHITECTURE.md](../../../ARCHITECTURE.md) (§8 online), [ADR-0005](../../adr/0005-online-architecture.md),
[ADR-0014](../../adr/0014-spike-c-steam-p2p-go.md), [ADR-0018](../../adr/0018-replay-format.md) (replays), then
[README.md](README.md) in this folder.
**Where:** `game/UI/Online/` (new screens), the online parts of `game/UI/` today (`Lobby*`, `PingIndicator`,
`PlayerNameLabel`, `MatchStatus*`), `game/Online/` (only to hand the screens what they need),
`tests/FightingGame.Tests/UI/Online/`. Steam access only through `Platform.Steam`.
**Depends on:** UI-01, UI-04 (flow). UI-05/06/07 for the shared character, stage and results screens.

## Goal

Turn today's placeholder lobby (host, join/invite, ready; `LobbyMenuItems`, `LobbyChoice`) into clear online screens,
and add the replay browser. The netcode and Steam lobbies exist; this is the player-facing side.

## Screens

- **Online menu:** Play a friend (invite / join), Private lobby (create / join by code if Steam lobbies allow),
  Replays. Quick match comes later when there's matchmaking; leave a disabled entry or nothing.
- **Lobby:** both players' Steam names and avatars, **connection quality** before starting (bars from `PingIndicator`
  / `PingQuality`, plus the ping in ms), each player's ready state, leave. The pre-match checks (version, data hash,
  match rules, ARCHITECTURE.md §8) show a clear message when they fail ("Your opponent has a different game version").
- **Character select online:** each player picks on their own PC (UI-05 with one local side); the remote pick shows
  when they confirm. Stage: pick one rule (host picks, or random) and say which in the PR.
- **During and after:** disconnect screen with the reason ("Opponent disconnected", "Connection lost"); rematch on the
  results screen (UI-07) waits for both, then starts again without going back to the lobby.
- **Replay browser:** list of saved replays (`ReplayFolder`): date, characters, colours, winner if known. Watch, delete
  (with confirmation). Replays from another game version are listed as "unsupported" and can't be played (ADR-0018).

## Design notes

- Screen state (lobby members, readiness, check results, replay list entries) is plain C#, tested without Steam or
  Godot. Steam calls stay behind `Platform.Steam` interfaces, so the screens work in dev without Steam (show "Steam
  not running" instead of crashing).
- Never hide a desync or a failed check (ARCHITECTURE.md §8).

## Done when

- Two PCs (or the direct dev path): invite → lobby with ping → both ready → character select → match → rematch.
- Failed version check shows a clear message and doesn't start the match.
- Replay browser lists, plays and deletes replays.
- Build, tests and format pass. Screenshots of each screen.

## Out of scope

Matchmaking, ranked, spectating, achievements.
