# ADR-0018: Replay file format and recording

- **Status:** Proposed (Oct 2026). Details ADR-0005 §7.

## Context

ADR-0005 §7 says a replay is a small binary file: header (version, data hash, rules, characters, colours) plus
compressed inputs. P2-10 has to fix the layout, decide which replays a build accepts, and record every match type,
including online ones where the other PC's inputs arrive late and frames get rolled back.

## Decision

1. **A replay plays only on the build that recorded it.** The reader rejects a different game version or data hash
   before reading the inputs, with a message the player understands (`ReplayRejectedException` + reason). No
   conversion between versions: the logic changes between builds, so old inputs would play a different match.
2. **Layout** (`src/Netcode/Replays/ReplayFormat.cs`, little-endian): magic `FGRP` and a format number uncompressed;
   then, zlib compressed: game version, data hash, `MatchRules.ComputeHash()`, the whole `MatchRules` struct, P1's
   and P2's character id and colour, frame count, and the inputs as runs of identical frames (run length, P1 word,
   P2 word). Damaged files (cut off before the last input, zlib checksum or rules hash wrong) are rejected, never
   half-played.
3. **The header carries the whole `MatchRules`,** not only its hash, so a replay recorded under rules a developer
   tweaked plays back under those rules. The hash is the same one the online handshake compares (P1-20).
4. **Recording wraps the game logic, not the driver:** `RecordingMatchSimulation` sits around
   `ComposedMatchSimulation`, so it sees both players' inputs in every match type. Each tick records at
   `state.Frame`; a resimulated frame replaces its predicted inputs and drops the frames after it.
5. Compression is the BCL's `System.IO.Compression`: no new dependency. No new settings: whether and where replays
   are saved is `game/` work (player setting once there is a replay menu).

## Consequences

- Replays break with every new build. Fine for development and sharing within one version; a replay archive across
  versions would need the old build (or a re-simulation tool), decided when a replay menu exists.
- The rules bytes are raw struct memory: same build means same layout, and every target (x64, Steam Deck) is
  little-endian.
- An online recording's last few frames may still hold predicted inputs if the match stops before they're confirmed;
  the composition root should save after the match is over and the session has caught up.
- The format number only changes with the layout; the game version check does the rest.
