# ADR-0033: A press that starts an action is used up

- **Status:** Proposed (Oct 2026), awaiting both devs

## Context

Since the hitstop input fix, the button buffer and motion windows count the fighter's own frames (ADR-0002), but
nothing marked a press as used. A buffered press could start two actions in a row: a move, then a cancel out of it
when a window opens while the press is still in the buffer (more likely now that hitstop doesn't age it). A global
freeze had to keep aging input only because the Roman Cancel module relied on its freeze aging out the a+b+c press;
with a 1-frame freeze that press also started 5a or a throw. SF6 and Strive use a press once and keep input through
freezes.

## Decision

1. **A press that starts an action is used up and can't start another.** Actions: a move or cancel the state
   machine starts (throws included), a dash or backdash, an air dash or double jump, and a module cancel request
   the core accepts (Roman Cancel, burst). A jump or run from a held direction uses nothing. A fixed core rule, not
   a setting.
2. **Per fighter, all presses up to now:** the input history (`InputHistory`, flat state) stores how many frames
   ago its newest used frame was; `UsePresses()` marks everything recorded so far, `IsUsed(framesAgo)` asks. A press
   or motion trigger on a used frame doesn't count (`ButtonPress.IsUnusedPress*`, `MotionParser`); the directions
   before it still do, so one motion can feed two specials with two presses. Pressing a button again always works.
   Simpler than per-button tracking; a second button pressed in the same buffer window is used up with the first.
3. **Who marks:** the state machine step after a fighter's machine started an action from a press; the request
   applier when it accepts a cancel request (module cancels are press-triggered; a module that ever needs one that
   isn't asks for an ADR change). No new core request.
4. **Checks that start nothing read every press:** throw tech (an option-select tech with a throw input is standard
   practice), instant block (direction timing), pushback guard (held buttons; its move lock already stops attacks).
5. **Global freezes no longer age the frozen fighters' input** (`TimeControl` marks the frame as skipped, like
   hitstop): a press made during a Roman Cancel or super freeze comes out right after it.

## Consequences

- Roman Cancel works with any freeze length; its remark and ADR-0002/ADR-0011 note this rule.
- The input history grows by one int: the state layout and golden replay checksums changed.
- Throw tech never uses up presses; that's safe while a tech's recovery outlasts the button buffer. If techs ever
  get shorter recovery or hitstop, `Sim.Throws` should use the presses up too.
