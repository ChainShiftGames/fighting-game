# ADR-0005: Online architecture

- **Status:** Accepted (Oct 2026); §1 route amended by ADR-0014
- **Gaps:** ROADMAP GAP-8, GAP-16 to GAP-21, GAP-23, GAP-24

## Decision

1. **Steam first.** Steam integration uses **Steamworks.NET** (plain C#, MIT). Online traffic goes through
   **Steam's relay** (hides IP addresses, handles NAT), plugged into Backdash as a custom transport. Direct UDP
   is only for development testing. Malformed packets are dropped.
2. **One match-driver interface** for local, online, replay, training and test matches. Backdash sits behind it
   and can be replaced or forked.
3. **Effects during rollback:** the logic emits events (hit, sound, spark…) tagged with their frame. Presentation
   plays each event once and cancels effects whose event disappears after a rollback.
4. **Network defaults:** automatic input delay of 1–2 frames based on ping (player can override 0–4); warning
   after 3 s without data, match ends without a result after 10 s. Test conditions: 100 ms ping, ±20 ms jitter,
   2 % packet loss.
5. **Desyncs:** a checksum of the game state every frame, compared between players. On a mismatch both sides save
   a dump (state, inputs, version) to a folder; a dev tool shows what differs. During development the two devs
   exchange these files manually.
6. **Handshake:** before a match, players exchange game version, game-data hash and match rules. A mismatch
   shows "versions differ — update the game".
7. **Replays:** a small binary file with a header (version, data hash, rules, characters, colours) and the
   compressed inputs.
8. **Match rules** (toggleable mechanics and gameplay values; ADR-0011) are **locked for players**. Shipped builds
   use one fixed rule set baked into the release data; players can't view or change it. Only during development
   can the devs tweak rules (debug panel; changing them restarts the round). The handshake still compares the
   rules hash, so a modified or dev build can't join a normal match.
