# ADR-0010: Attack button layout A B C D, defined in data

- **Status:** Accepted (Oct 2026)
- **Closes:** ROADMAP §8 Q3

## Decision

1. Four attack buttons: **A, B, C, D**. Plus the dedicated **Special** and **Dash** buttons (CURRENT_STATE §3 Controls).
2. The layout is **data, not code** (**developer setting**): a button-set file in `data/` lists the buttons and
   their bit positions in the input word. What each button does is defined per move in character data.
3. The 16-bit input word: 4 direction bits + 6 button bits (A, B, C, D, Special, Dash) + **6 spare bits** for
   future buttons.
4. No game logic may branch on a specific button name or count. Motions, moves, throws, cancels and the CPU bot
   refer to buttons through the button set.
5. Player bindings map devices to these buttons (**player setting**); the UI lists buttons from the button set.

## Consequences

- Adding, removing or renaming a button is a data change plus a schema version bump, not a refactor.
- Changing the button set changes the data hash, so replays and online matches stay consistent automatically.
- A scenario test with a 5-button set guards against hard-coded assumptions.
