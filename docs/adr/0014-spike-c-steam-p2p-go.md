# ADR-0014: Spike C: Steam peer-to-peer for online matches

- **Status:** Proposed (Oct 2026). Amends ADR-0005 §1 (route of online traffic).

## Context

Spike C (P0-20, P0-21) had to show that online matches work through Steam before Phase 1 builds on it. ADR-0005 §1
chose Steamworks.NET as a custom Backdash transport and said all traffic goes through Steam's relay, hiding IP
addresses; it asked for this go/no-go with measured latency. P0-13 showed the toy sim is playable at
100 ms / ±20 ms / 2 % loss.

## Measurements

Two devs on separate home networks (Steam friends), AppID 480, friends-only lobby joined by a Steam invite, input
delay 2 f, rollback window 8 f, no simulated lag. Readings from the F3 overlay and log (P0-22), sampled once a second
over 150–180 s on both PCs. Steam chose a **direct** route (see Problems 1).

- **Steam's own ping:** avg 22 ms (17–26 ms). Packets delivered: 99–100 %.
- **Backdash's ping (game to game):** avg 39–46 ms, max 65–66 ms (ignoring the first sample, 4–5 ms, taken before
  the peers settle).
- **Frames behind:** 1–3 f either way.
- **Rollbacks:** 16/min (avg 1.0 f, longest 1 f) on one PC, 54/min (avg 1.1 f, longest 4 f) on the other. They
  follow how often the *other* player changes input.
- **Feel (P0-21 test):** invite flow was easy; matches "felt great".
- **Relay route:** not measured (friends get the direct route; see Consequences).

## Problems found

1. **Steam connected the two friends directly, not through the relay.** Networking Messages uses Steam's
   peer-to-peer setting: each player chooses in the Steam client who may see their IP address ("Peer-to-Peer
   Networking and Sharing Your IP Address"); by default only friends do, and other connections are relayed. So
   ADR-0005's "always relayed" was never what Steam does by default.
2. **Backdash rejects port 0** even when the socket ignores it; the first two-dev test crashed. Fixed in P0-21 (a
   placeholder port), covered by `PortlessTransportTests`.
3. **Steam IDs ride in Backdash's `SocketAddress` padding** (Backdash identifies peers by address bytes). Works with
   0.7.8-preview; a Backdash upgrade must rerun `PortlessTransportTests` and a Steam match.
4. **Backdash's ping is ~20 ms above Steam's.** Likely frame-paced handling: each side reads packets once per 16.7 ms
   frame. Not broken down yet; matters for the automatic input delay (ADR-0005 §4), which should use Backdash's ping.
5. **Invites only reach a running game.** Under AppID 480 Steam can't launch the game into a lobby; the launch
   argument (`+connect_lobby`) isn't handled yet.
6. **AppID 480 is shared** (Spacewar): fine between the devs, not for outside playtests.

## Decision

1. **Go: Steam Networking Messages stays the online transport**, behind `PeerTransport` (ADR-0005 §2).
2. **Route: Steam decides, as in most peer-to-peer fighting games.** Direct between players whose Steam setting
   allows it (by default friends), Steam's relay otherwise. The game doesn't override the player's Steam setting.
   This replaces ADR-0005 §1's "all traffic through Steam's relay". Direct UDP stays development-only.
3. **Revisit for matchmaking with strangers** (ranked/casual queues, later phase): measure the relay then, and decide
   whether those matches force the relay (Steam lets a game do that) in their own ADR.
4. **Keep the connection readings** (F3 overlay and log, showing direct vs relayed) as a **developer setting**.

## Consequences

- Phase 1 can build online features on `SteamTransport` and the lobby.
- A friend's IP address may be visible to the other player, as with other P2P fighting games; players control it in
  Steam.
- Before playtests outside the two devs: own Steam AppID, and handle `+connect_lobby` so an invite can start the game.
- Automatic input delay (ADR-0005 §4) reads Backdash's ping; break down problem 4 when it's built.
- Upgrading Backdash reruns `PortlessTransportTests` and a two-PC Steam match (problem 3).
