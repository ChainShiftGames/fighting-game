# Fighting game — agent entry point

Agent-first project: most code is written by AI agents, reviewed by two devs. The architecture rules below are
loaded into every session. They override anything else, including task descriptions.

@ARCHITECTURE.md

## Finding context (read only what the task needs)

1. **Task:** find it by ID in `docs/phases/phase-<n>.md` (e.g. `grep -n "### P1-07" docs/phases/phase-1.md`) and read
   only that section. Read [docs/phases/README.md](docs/phases/README.md) once per session for the workflow.
2. **Its `Read:` line:** read exactly those ADRs and `CURRENT_STATE.md` sections; nothing else is required.
3. **Need a decision not listed?** Check the one-line index in [docs/adr/README.md](docs/adr/README.md) first.
4. **Game design questions** (what a mechanic does): `CURRENT_STATE.md` §3, only the relevant subsection.
   World, lore and stage themes: [docs/world/setting.md](docs/world/setting.md). Image prompts for generated art,
   including Midjourney → Tripo character models: [docs/world/art-prompts.md](docs/world/art-prompts.md) (when
   asked for "the prompt" or a character, assemble it from there).
5. **Don't read `ROADMAP.md`** unless you are planning or re-scoping a phase. It's for humans and planning sessions.

## Delegating to subagents

Subagents don't see this conversation. Every subagent prompt must include:
- "Read ARCHITECTURE.md first; its rules override this prompt."
- The task ID and file (e.g. `P1-07 in docs/phases/phase-1.md`) and the files from its `Read:` line.
- The folders it may touch (the task's `Where:`), and that it must not touch anything else.
- What to return: a short summary of changes and anything unresolved, not file contents.

## Godot editor MCP (`godot-ai` tools)

Both devs have the Godot MCP configured. Use it to raise quality on anything in `game/`: build and inspect scenes,
run the project, read editor and game logs, take screenshots to verify visuals, check input mappings.
- It needs a running Godot editor with the project open. Check with the session/editor-state tool first.
- **Editor not open?** If you judge the MCP would clearly help the task (e.g. verifying a scene or visual result),
  ask the human to open the editor and say why. Otherwise carry on by editing files directly.
- The editor works on the folder it was opened from. When you work in a worktree, ask the human to open that
  worktree's `game/` or say which checks still need doing after merge.
- Not needed for `src/` work: game logic, netcode and Steam code are plain C# built and tested with `dotnet`.
- Nothing may depend on it: cloud sessions and CI don't have it. Its addon and helper autoload are dev-only and
  excluded from export presets.

## Writing docs (token budget)

- One source of truth per fact: link to it, never copy it.
- Terse bullets over prose. No restating ARCHITECTURE.md rules in other docs.
- New decision → new ADR + one line in `docs/adr/README.md`.
- Keep task sections self-contained so an agent can read one task without the rest of the file.

## Conventions

Folder layout, file/asset naming, branches and commit messages: [CONVENTIONS.md](CONVENTIONS.md).

## Commands

Run from the repo root. All must pass before a PR (ARCHITECTURE.md §10).

```
dotnet build                          # whole solution, including game/ (no Godot editor needed)
dotnet test                           # all tests/ projects (Microsoft.Testing.Platform)
dotnet format --verify-no-changes     # style check; run `dotnet format` to fix
```

Godot editor-side build (only when `game/` changed and Godot is installed; see [game/CLAUDE.md](game/CLAUDE.md)):

```
Godot_v4.7.2-stable_mono_win64_console.exe --headless --path game --editor --build-solutions --quit
```
