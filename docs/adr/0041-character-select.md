# ADR-0041: Character select: colours and descriptions on the presentation side, two-player screens

- **Status:** Proposed (Oct 2026)

## Context

UI-05 ([brief](../tasks/menus/05-character-select.md)): both players pick a character and a colour at once. No
character has colour or description data yet, and the menu framework ([ADR-0039](0039-menu-framework.md)) only has
one cursor per screen and one list-drawing view.

## Decision

1. **Colours and the one-line description live in a select file beside the art file**,
   `game/Assets/Characters/<id>/<id>_select.json` (`CharacterSelectFile`): `selectVersion`, `description` (a
   translation key in `menu_text.csv`) and `colours` (1–256 `#rrggbb`). Presentation only, like the art file
   ([ADR-0028](0028-fighter-models-and-contact-frames.md)): **not in `data/`**, so the data hash and match rules don't
   change when art does. **Developer setting.** No file = one colour, no description; an invalid file is logged and
   shown as that placeholder (as the art file does). The name stays `displayName` from the character data.
2. **The colour index is what travels**: `FighterPick.Colour` → `MatchLineup` → `ReplayPlayer.Colour` in the replay
   header. Its look is looked up from the select file at draw time. Colours don't affect game logic.
3. **Selection rules are plain C#** (`CharacterSelection`, tested): steps character → colour → ready; back undoes one
   step, and on the grid leaves the screen once the other player is back on the grid too. **Mirror:** whoever readies
   second can't take the first's colour (cycling skips it, a cursor on it is shifted to the next free one); a
   one-colour character is shared. **Random** picks character and free colour with presentation randomness
   (`System.Random` in `game/`), never the logic PRNG, and readies at once. **Session memory** (`PickMemory`): each
   player's cursor and colour start on their last pick. "Both ready" shows for `BothReadyMilliseconds` (900) before
   stage select, so a back can still undo it. Grid columns (6) and this time: **developer settings** in
   `CharacterSelectSettings.Default`.
4. **A player with no controller mirrors**: the training dummy, and player 2 in versus with only the keyboard, play
   the other player's character in the next free colour. Chosen over "P1 picks the dummy too" because it is the
   fastest way into training; picking the dummy belongs to the training menus (UI-13).
5. **Devices per player** come from the `PlayerDevicesChoice` assignment the match will use (`AssignedMenuPlayers`);
   a default assignment claims a pad plugged in on the screen the way the match would. Side select (UI-03) replaces
   the source, not the screen.
6. **Two-player screens**: `IMenuScreen.TakesEveryPress` (default false) makes the navigator hand every press,
   back and directions included, to the screen with its device. **Custom-drawn screens** get their own view node
   next to `MenuView`; `GameMenus` shows that one instead of `MenuView` while the screen is on top
   (`CharacterSelectView`).

## Consequences

- The fighter in a match isn't tinted yet: applying the colour (palette swap or material tint from the select
  file) is a presentation follow-up once there is art to recolour.
- A portrait or preview model can be added to the select file later; today the preview is the name on a card in the
  colour shown.
- Online character select (UI-14) can reuse `CharacterSelection` with one local side choosing.
