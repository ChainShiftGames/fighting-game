# ADR-0044: Release version numbers, tags and changelog

- **Status:** Proposed (Oct 2026)

## Context

Until now no project set a version: every build reported the .NET SDK default `1.0.0+<commit>` (`GameVersion`), with
nothing in `project.godot` or the export presets. Playtest builds and bug reports need a number people can read and
compare, and releases need a record of what changed.

## Decision

1. **Semantic version `MAJOR.MINOR.PATCH`, set once:** `<Version>` in `Directory.Build.props`. Starts at `0.1.0`.
   While below 1.0: **minor** for a build with new features (a playtest or Steam build), **patch** for a fixes-only
   build. 1.0.0 is the first public release.
2. **Godot repeats it:** `application/config/version` in `game/project.godot` (Godot can't read MSBuild). The export
   presets leave `application/file_version` / `product_version` empty, so the Windows export takes the project's.
   `GameVersionTests` fails the build's tests when the two differ or a preset sets its own.
3. **Online compatibility stays per commit:** `GameVersion` is still `<version>+<commit>`, and the handshake and
   replays compare all of it (ADR-0005 §6). The version number is for people, not for matching builds.
4. **Every release is an annotated git tag `v<version>`** on the `main` commit it was built from. Exported builds come
   from a tag. Steps: [CONVENTIONS.md](../../CONVENTIONS.md) "Releases".
5. **`CHANGELOG.md`** at the repo root: one section per release, newest first, player-facing bullets. It's written
   when releasing, from the PRs merged since the last tag, not in every PR: parallel PRs would all edit the same lines
   and conflict.
6. No new settings and no dependency.

## Consequences

- A release is one PR (version bump in two files + changelog section), merged, then tagged.
- The version can be shown in the game later (title or main menu corner) by reading `GameVersion`; not built yet.
- A CI job building exports from tags can come later; nothing depends on it.
