# Tools

Developer tools (ARCHITECTURE.md §2): desync diff, speed benchmark, replay inspector, and `Analyzers/` (ADR-0009).
Each tool is added by the task that needs it.

- `DataMigrate/`: upgrades data files to the current schema versions, one step per format change (ADR-0021).
  `dotnet run --project tools/DataMigrate -- data tests/Sim.Data.Tests/Fixtures/data tests/Scenarios/Fixtures/data`;
  `--check` lists files that need it; `--format <file>…` rewrites files in the shared layout.
- `Blender/vrm_to_glb.py`: VRoid `.vrm` → `<id>.glb` + one toon material (`game/Assets/Shaders/toon*.gdshader`)
  per part, for Spike B (ADR-0027/0028). Needs Blender 4.2+ with the "VRM format" extension. Usage in the file;
  after the first import, map the `.glb`'s materials to the `.tres` files in its `.import` (`_subresources`).
- `Blender/tripo_to_glb.py`: a rigged AI character (Tripo's rigged FBX, or a Mixamo rig; Tripo's rigged `.glb` is
  broken) → cleaned `<id>.glb` with toon-smoothed normals, a toon material and a Mixamo bone map. Usage in the file;
  map the material and bone map in the `.glb`'s `.import` as for VRoid.
- `Blender/prop_to_glb.py`: an unrigged AI prop (Tripo `.fbx`/`.glb`) → a centred, 1 m, decimated `<id>.glb` plus
  its textures, e.g. a projectile's model (`ProjectileModel`).
