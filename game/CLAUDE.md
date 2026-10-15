# game/ (Godot 4.7.2 .NET) — local gotchas

Rules for this folder: ARCHITECTURE.md §9 (presentation) and §6 (settings). Versions: ADR-0001.

- **Two builds.** Root `dotnet build` compiles `FightingGame.csproj` (Godot.NET.Sdk from NuGet, no editor). The
  headless editor build in the root `CLAUDE.md` Commands also runs Godot's import and C# glue; use it to check
  that the editor accepts the project. The exe isn't on `PATH` by default: use its full path.
- **Keep `net10.0`** in `FightingGame.csproj`; Godot.NET.Sdk defaults to net8.0.
- **Commit** `.tscn`, `.tres`, `.import` and `*.uid` files; `.godot/` is generated and ignored.
- **`project.godot` and `export_presets.cfg` are editor-written.** Prefer the editor or the `godot-ai` MCP; keep
  hand edits minimal.
- **`addons/godot_ai/` is dev-only** (ADR-0012): never reference it from game code or scenes; export presets keep
  excluding it. Telemetry off via `GODOT_AI_DISABLE_TELEMETRY=1` (user environment variable).
- **`res://` paths are case-sensitive in exports** (Steam Deck is Linux): match file names exactly.
