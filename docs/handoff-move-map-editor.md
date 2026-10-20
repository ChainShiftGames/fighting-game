# Handoff: character editor rebuild as the Move map (option C)

Status: **in progress.** The new pieces are merged but not wired in yet, so the Character tab still shows the old
editor. Everything builds, `dotnet format` is clean and all tests pass.

- Design: option C in [mockups/character-editor-options.html](mockups/character-editor-options.html) (open it in a
  browser). The user asked for **C, 1:1 visually and functionally**.
- Before calling it done, you **must** verify in the Godot editor that you can change a move's frame data and a
  projectile's spawn frame and speed, then save.
- Where: `game/addons/character_editor/` only (plus its tests). Read ADR-0007 first. Delete this file when the work
  lands.

## Done (commit "WIP: move map …" and the one after)

Engine-free, unit-tested in `tests/FightingGame.Tests/Addons/CharacterEditor/Map/MoveMapTests.cs`:
- `Map/MoveMapSettings` + `move_map.json` (developer setting, editor only): columns of normals by button, plain
  names for buttons, stances and special motions, and the states that count as movement.
- `Map/MoveNames` (5b → Standing Medium) and `Map/MoveGlyphs` (236 → ↓↘→, keycaps).
- `Map/MoveChains` + `ChainLink`/`ChainTargetKind`: chain arrows read from cancel windows (`into`,
  `intoCategories`, `intoJump`).
- `Map/MoveMapLayout` + `MapColumn`/`MapGroup`/`MapLens`: columns per lens (Combos, Special moves, Everything),
  the jump entry, and category boxes.
- `Map/MoveFacts`: when the move comes out, damage, stun, and advantage = stun − frames left after the last hit
  (as in `tests/Scenarios/Combat/frame_advantage_*`). Projectiles and knockdowns: null.
- `Map/ProjectileFields`: spawn frame, speed forward/up (subpixels), lifetime, durability.
- `Map/PointBlankOpponent`: the idle hurt boxes, mirrored, touching the move's body (the dashed ghost).

Godot side (`#if TOOLS`, compiles; **never run in the editor yet**):
- `Screen/Look/`: `MapPalette`, `MapTheme` + `ThemePaint` (the whole theme; set it on the screen root),
  `MapLook` (fonts and scale read from the theme), `ShapeArt`, `KeycapArt`, `GradientButton` (Save).
  Marcellus comes from `game/Assets/UI/Fonts/`. Body and mono fonts are `SystemFont`s, so no new files.
- `Screen/Map/`: `MoveMapCanvas` (cards, arrows, pan/zoom/fit, right-click menu → `MapMoveAction`), `MapPainter`,
  `MapPlacement`, `MapEdges`, `MapCards`/`MapCard`, `MapLegend`, `FrameStrip`.
- `Screen/Drawer/`: `MoveDrawer` (preview + header + tabs, or the character file), pages implementing
  `IDrawerPage`: `OverviewPage` (facts, kind, input, all settings via `SchemaForm`), `TimingPage` (wind-up/
  hitting/cool-down steppers, move-ranges toggle, `FrameTimeline`, `ProjectileCard`s), `HitAreasPage`
  (`BoxPanel`), `ChainsPage` (chips + `CancelPanel`). Plus `NumberStepper`, `UnderlineTabs`, `FactList`,
  `PhaseBar`, `InputLine`, `SectionTitle`, `DrawerMove`, `DrawerFacts`.
- `Screen/MapTopBar` (character picker, lens segments, status chip, ••• menu → `MenuAction`, Save) and
  `Screen/ProblemPopup`.
- Changed: `BoxPanel` no longer holds its canvas (`Canvas` is placed by the owner, the drawer's preview); its
  controls flow so they fit in the narrow drawer. `BoxCanvas` gained `Editable`, `Clicked`, `Opponent`,
  `ResetView()` and a gradient backdrop. `CancelPanel` gained `NameOf` for plain target names.

## Left to do

1. **Rewrite `Screen/CharacterEditorScreen`** around the new parts. Keep its public surface
   (`Build(dataRoot)`, `UnsavedCharacter`) so `CharacterEditorPlugin` stays unchanged:
   - `Theme = MapTheme.Build(EditorInterface.Singleton.GetEditorScale())` on the root.
   - Layout: `MapTopBar` on top. Below it an `HSplitContainer`: left a VBox with `MoveMapCanvas` (expand) and
     `FrameStrip`, right the `MoveDrawer`, about 340 px at 1× and resizable.
   - Settings: `MoveMapSettings.Parse(File.ReadAllText(ProjectSettings.GlobalizePath("res://addons/character_editor/move_map.json")))`.
   - On open or any edit: `links = MoveChains.Of(doc)`, `layout = MoveMapLayout.Of(doc, settings, lens, links)`,
     `categories` (shown id → category), `new MapPlacement(layout, categories)`, `MapCards.Of(...)`, then
     `canvas.ShowMap(...)`. Refit only for a new character or lens.
   - Selection: canvas `MoveSelected` → `drawer.ShowMove(new DrawerMove(...))`, `strip.ShowMove(name, root)`,
     `canvas.Select(id)`. Start on the first card of the first column.
   - Frames: `drawer.FrameShown` → `strip.MarkFrame`; `strip.FrameClicked` → `drawer.SelectFrame`.
   - Edits: `drawer.Edited` → rebuild the cards and strip, update the top bar state, and queue one deferred
     `saver.Validate(doc)` that feeds the chip, `ProblemPopup` and the cards' problem dots.
   - Top bar: `CharacterChosen` (with the old unsaved-changes discard dialog), `LensChosen`, `ProblemsPressed` →
     `ProblemPopup.OpenUnder`, `SavePressed` → `saver.Save`. Menu and card menu → `MoveFileActions`
     (`AskNew`/`AskDuplicate`/`AskDelete`, then select the id at `Done`'s index), priority via
     `doc.SwapMoves(i, i ± 1)`, `CharacterFile` → `drawer.ShowCharacterFile`, `Reload`.
   - `drawer.ShowCharacter(id, moveIds)` once per opened character (it loads the model).
2. Delete `MoveList`, `MoveEditor`, `CharacterToolbar`, `FrameDataPanel` once nothing uses them.
3. Restyle `FrameTimeline` to `MapPalette` phase colours and the theme font, and give `ChainsPage` chips the
   glow text colour.
4. **Verify in the Godot editor** (open this repo's `game/`; the godot-ai MCP can screenshot the Character tab).
   Compare with mock C and check:
   - On Testman, change 5B's frame data and 236C's projectile "Appears on frame" and "Speed forward".
   - Save, reload and confirm the JSON on disk.
   - Validation chip, lens switch, pan/zoom/fit, right-click menu, and the Hit areas tab drawing boxes on the
     preview.
   - Untested so far: theme item names, `MapLegend` anchoring, `NumberStepper` focus behaviour, `ResetView` on
     resize, `ProblemPopup` placement.
5. Docs: update the P3-40..42 notes in `docs/phases/phase-3.md` if behaviour changed. Consider an ADR line for
   the editor's own theme and the `move_map.json` developer setting (ARCHITECTURE.md §11).

## Decisions made

- No move display-name field in the schema: names come from notation plus `move_map.json`. Adding an optional
  `name` to `move.schema.json` would need an ADR, so it was left out.
- Map card colours: wind-up amber, hitting rose, cool-down blue (the mock), used everywhere in the new editor.
  The training frame meter keeps its own colours.
- Dimming: cards not joined to the selected move by an arrow are dimmed.
