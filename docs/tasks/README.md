# Task briefs

Work is built piece by piece. Each piece of work is one brief file an agent can pick up on its own.

- **Layout:** `docs/tasks/<area>/<nn>-<short-name>.md`, plus a `README.md` per area with the context its briefs share.
- **IDs:** area prefix + number from the file name (`menus/01-menu-framework.md` → `UI-01`). Used in branch names and
  commit summaries ([CONVENTIONS.md](../../CONVENTIONS.md)).
- **Each brief has:** `Read first:` (the docs and ADRs to read, nothing else needed), `Where:` (the only folders it may
  touch), `Depends on:`, goal, what to build, design notes, `Done when`, `Out of scope`.
- **Order:** the area README shows which briefs depend on which and which can run in parallel.
- Delete a brief, or mark it done at its top, once it's merged.

## Areas

| Area | Prefix | Covers |
|---|---|---|
| [menus](menus/README.md) | UI | Menu framework, controller side select, flow, select screens, settings, rebinding, online screens |
