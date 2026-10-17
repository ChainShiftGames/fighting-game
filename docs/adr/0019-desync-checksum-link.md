# ADR-0019: Desync checksums over the match transport

- **Status:** Proposed (Oct 2026). Details ADR-0005 §5; wire format alongside ADR-0014's transport.

## Context

P2-09 built the per-frame desync check (`DesyncMonitor`: compare every final frame's checksum, dump on the first
mismatch) behind `IChecksumLink`, which must deliver each side's checksums in frame order and never drop one
silently. Online matches need a link over the real match transport (UDP, Steam Networking Messages, the in-memory
and lag-simulating test transports), which today carries only Backdash packets and the handshake's hello. Backdash's
own consistency check samples one frame a second, has no dump and stays as a second line.

## Decision

1. **A second message kind on the match transport.** `ChecksumMessage` (`src/Netcode/Transport/`), big-endian:
   marker `0x4643` ("FC", no Backdash type and not the hello's "FG"), format byte, acknowledged frame, first frame,
   count (≤ 128), then `count` 64-bit checksums of consecutive frames. 12 + 8 × count bytes, at most 1,036, under
   `ITransport.MaxPacketSize`. `TransportSocket` hands these packets to the link instead of Backdash; malformed ones
   (and any while no link is attached) are counted as malformed like other junk.
2. **Unreliable, redundant, with a piggybacked cumulative acknowledgement.** Each packet repeats *every* checksum the
   other PC hasn't acknowledged yet and acknowledges how far this PC has received the other's in order. A lost or
   reordered packet is covered by the next one; duplicates and gaps are dropped on arrival, and the receiver passes
   checksums on in frame order, each once. No resend timer while the match runs, no extra ack packets.
   - *Why not "the last K checksums" without acks (the alternative considered):* a loss burst longer than K frames
     would leave frames uncompared for good: either the comparer's ring fails ("history too short") or a frame
     goes unchecked, which `IChecksumLink` and ARCHITECTURE.md §8 rule out. The acknowledgement costs 4 bytes per
     packet and makes delivery complete for any loss that the connection itself survives; the repeated window is
     usually the round trip in frames (~6–10 checksums, ~60–90 bytes at 100 ms).
3. **One packet per final frame, sent off the frame thread.** The game thread only stores the checksum; the packet
   is built and sent from the thread pool (no allocation), as Backdash sends from its own thread. A socket send on
   the frame thread doubled the measured peer frame.
4. **After a desync, keep sending.** The PC that finds it stops running frames, so nothing else would repeat its
   last checksums: the link resends them every 100 ms for up to 2 s until acknowledged, so the other PC finds the
   desync too and writes its own dump (both dumps are what `DesyncDiff` compares).
5. **Only final frames are compared:** frames that ran with both players' confirmed inputs, read from Backdash's
   confirmed-input listener (`ConfirmedInputFrames`) and the driver's own frame count (`ConfirmedFrame`).
6. **One desync path for the game.** A mismatch from Backdash's consistency check also goes through the monitor,
   which writes a dump for that frame; either way the driver throws one `DesyncDetectedException` with `DumpPath`,
   and throws it again on every later frame without writing another dump.
7. **Settings.** The history length (`desync.historyFrames` in `data/netcode/online.json`, default 600) is a
   **developer setting**; it also bounds how far one side's checksums may run ahead. The dump folder (`user://desyncs`)
   is a **developer setting** too, fixed by the game. The check has no switch: it is always on (ARCHITECTURE.md §8).

## Consequences

- One small packet per frame each way on top of Backdash's (~5 KB/s at 100 ms ping); measured cost per frame on the
  dev PC: ~8 µs for the hash on the frame thread, no allocation (tools/Benchmarks/README.md).
- The format byte only changes with the layout; different builds never play each other (ADR-0005 §6), so no version
  negotiation is needed.
- A Backdash upgrade doesn't affect this format, but a new Backdash message type equal to `0x4643` would; the marker
  sits far above Backdash's 0–10.
- Dumps from Backdash's check carry 0 as the other PC's checksum: Backdash compares its own hash, not ours.
