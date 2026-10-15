# ADR-0012: Test framework and the Godot editor MCP addon

- **Status:** Accepted (Oct 2026)

## Context

P0-02 needs a test framework, and the devs already use the Godot AI editor MCP (CLAUDE.md). ARCHITECTURE.md §11
requires an ADR for every NuGet package and Godot addon.

## Decision

1. **xUnit v3** (`xunit.v3` 4.0.2, Apache-2.0) is the test framework for every project in `tests/`. Versions are
   pinned in `Directory.Packages.props`.
2. Tests run on **Microsoft.Testing.Platform** (`"test": {"runner": ...}` in `global.json`), the .NET 10 default
   for `dotnet test`. No `Microsoft.NET.Test.Sdk` / VSTest adapter.
3. **Godot AI addon** (`game/addons/godot_ai/`, v4.3.0, MIT) is vendored for development only:
   - every export preset excludes `addons/godot_ai/*`; the addon's own export plugin strips its
     `_mcp_game_helper` autoload from exported builds;
   - nothing in `game/` may depend on it (CI and cloud sessions don't have an editor);
   - its telemetry is off: each dev sets the user environment variable `GODOT_AI_DISABLE_TELEMETRY=1`
     (telemetry is a per-user editor setting, so the project can't turn it off for everyone).
   - Upgrades are deliberate PRs that replace the whole folder.

## Consequences

- ArchUnitNET (ADR-0009) uses its xUnit v3 integration.
- `dotnet test` needs no extra flags; test projects are executables.
- The addon adds ~300 GDScript files to the repo; they are excluded from exports and from our C# build.
