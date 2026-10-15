# ADR-0004: Fixed 60 Hz game loop and gameplay camera

- **Status:** Accepted (Oct 2026)
- **Gaps:** ROADMAP GAP-6, GAP-13, GAP-14

## Decision

1. We run our **own fixed 60 Hz loop** (not Godot's physics tick). Godot's frame callback only feeds it time.
2. Controller input is read **immediately before** each logic frame, to keep input latency minimal.
3. Display defaults to **60 fps with vsync on**; a setting turns vsync off for lower latency. On 120/144 Hz
   screens the logic still runs at 60 Hz.
4. Online, Backdash's time sync slightly adjusts pacing so both players stay in step.
5. **Camera:** its horizontal position and the maximum distance between players are part of the game logic
   (they affect walls and corners). Zoom, shake and flashes are presentation only.

6. **Input latency** (GAP-14) is a top priority: as low as possible. A built-in counter measures the time from
   reading the input to the frame being shown, and it's tracked at every milestone. No external camera tests.

## Consequences

- Input latency stays under our control and is measured automatically.
- Camera code splits into a logic part (in the sim) and a visual part (in Godot).
