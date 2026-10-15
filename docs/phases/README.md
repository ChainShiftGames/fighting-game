# Phase task lists

Actionable work for agents and subagents, one file per roadmap phase. [ROADMAP.md](../../ROADMAP.md) says *what*
each phase delivers and *why*; these files say *how*, as tasks small enough for one PR each.

| Phase | File | Detail level |
|---|---|---|
| 0 — Foundation & spikes | [phase-0.md](phase-0.md) | Full |
| 1 — Simulation core, offline | [phase-1.md](phase-1.md) | Full |
| 2 — Online | [phase-2.md](phase-2.md) | Full |
| 3 — Systems & the Lab | [phase-3.md](phase-3.md) | Full |
| 4 — Vertical slice | [phase-4.md](phase-4.md) | Outline; detailed at phase start |
| 5 — Roster & content | [phase-5.md](phase-5.md) | Outline |
| 6 — Online features & Steam | [phase-6.md](phase-6.md) | Outline + decisions to make |
| 7 — Beta, polish, launch | [phase-7.md](phase-7.md) | Outline + decisions to make |
| 8 — Post-launch | [phase-8.md](phase-8.md) | Outline + decisions to make |

Phases 4–8 are deliberately thin. Expand a phase into full tasks when the previous phase is close to done, so the
plan reflects what was actually built.

## How agents work through a phase

1. **Read [ARCHITECTURE.md](../../ARCHITECTURE.md) first.** Its rules beat anything in a task description.
2. **Pick a task** whose `Needs` are all done and whose status is `todo`. Set it to `in progress` with your branch
   name in the same PR that starts the work.
3. **One task = one branch = one PR.** Branch name: `p<phase>/<task-id>-<short-name>`, e.g. `p1/t07-character-schema`.
4. **Done means every "Done when" item is true** and CI is green. Tick the task (`done`) in the PR.
5. **Unclear or conflicting?** Stop and ask the devs; don't guess on architecture. If a decision is needed, propose
   an ADR.
6. Humans approve every PR and playtest every phase exit.

## Parallel work (subagents)

- Tasks marked **Parallel: yes** can run at the same time as other parallel tasks in the same group, because they
  touch different folders.
- Never give two agents tasks that touch the same files. When in doubt, run them one after the other.
- The **Needs** list is the dependency graph: a task can start once everything it needs is merged.

## Task format

```
### P1-07 Character data schema v1           ← ID and name
Status: todo | in progress (branch) | done
Needs: P1-02                                   ← tasks that must be merged first
Parallel: yes/no
Where: src/Sim.Data/, data/schema/             ← folders this task may touch
Settings: motion window (developer)            ← every setting with its audience (ARCHITECTURE.md §6)
- What to build, as bullets
Done when:
- [ ] verifiable checks
```

Every task implicitly also requires: ARCHITECTURE.md rules followed, tests for new behaviour, docs updated in the
same PR.
