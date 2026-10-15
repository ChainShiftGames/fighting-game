# ADR-0014: Spike C: Steam relay for online matches

- **Status:** Proposed (Oct 2026). **Relay numbers pending:** the measured match ran direct (see Problems 1); the
  relay-only rerun fills in "Relay" below before both devs decide.

## Context

Spike C (P0-20, P0-21) had to show that online matches work through Steam before Phase 1 builds on it. ADR-0005 §1
chose Steamworks.NET with all traffic through Steam's relay (no IP addresses exchanged, NAT handled) as a custom
Backdash transport, and asked for this go/no-go with measured relay latency. P0-13 showed the toy sim is playable at
100 ms / ±20 ms / 2 % loss.

## Measurements

Two devs on separate home networks, AppID 480, friends-only lobby joined by a Steam invite, input delay 2 f, rollback
window 8 f, no simulated lag. Readings from the F3 overlay / log (P0-22), sampled once a second.

- **Direct route (Steam chose ICE, 150–180 s, both PCs):**
  - Steam's own ping: avg 22 ms (17–26 ms). Packets delivered: 99–100 %.
  - Backdash's ping (game to game): avg 39–46 ms, max 65–66 ms (ignoring the first sample, 4–5 ms, taken before
    the peers settle).
  - Frames behind: 1–3 f either way.
  - Rollbacks: 16/min (avg 1.0 f, longest 1 f) on one PC, 54/min (avg 1.1 f, longest 4 f) on the other. They
    follow how often the *other* player changes input.
- **Relay (ICE off):** _pending rerun._ Same readings; the overlay must say "relayed".
- **Feel (P0-21 test):** invite flow was easy; matches "felt great".

## Problems found

1. **Steam connected directly, not through the relay.** Networking Messages tries ICE (direct P2P, which exchanges
   IP addresses) unless told not to. Fixed: `SteamRelayOnly` turns ICE off before the socket opens and fails loudly
   if Steam refuses.
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

_Proposed; decided by both devs after the relay rerun._

1. **Go: Steam Networking Messages through Steam's relay stays the online transport**, behind `PeerTransport`
   (ADR-0005 §1–2), if the relay rerun stays inside the conditions P0-13 proved playable (≤ 100 ms ±20 ms, ≤ 2 % loss).
2. **Relay only.** Direct P2P stays off in every build; no setting. Direct UDP remains development-only.
3. **Keep the connection readings** (F3 overlay and log) as a **developer setting**.
4. **Plan B** if the relay is too slow: an opt-in direct connection as a **player setting** that warns it shares the
   IP address, decided in its own ADR.

## Consequences

- Phase 1 can build online features on `SteamTransport` and the lobby.
- Before playtests outside the two devs: own Steam AppID, and handle `+connect_lobby` so an invite can start the game.
- Automatic input delay (ADR-0005 §4) reads Backdash's ping; break down problem 4 when it's built.
- Upgrading Backdash reruns `PortlessTransportTests` and a two-PC Steam match (problem 3).
