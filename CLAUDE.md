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
5. **Don't read `ROADMAP.md`** unless you are planning or re-scoping a phase. It's for humans and planning sessions.

## Delegating to subagents

Subagents don't see this conversation. Every subagent prompt must include:
- "Read ARCHITECTURE.md first; its rules override this prompt."
- The task ID and file (e.g. `P1-07 in docs/phases/phase-1.md`) and the files from its `Read:` line.
- The folders it may touch (the task's `Where:`), and that it must not touch anything else.
- What to return: a short summary of changes and anything unresolved, not file contents.

## Writing docs (token budget)

- One source of truth per fact: link to it, never copy it.
- Terse bullets over prose. No restating ARCHITECTURE.md rules in other docs.
- New decision → new ADR + one line in `docs/adr/README.md`.
- Keep task sections self-contained so an agent can read one task without the rest of the file.

## Commands

Added in P0-02/P0-06 (build, test, format check). Until then there is no code.
