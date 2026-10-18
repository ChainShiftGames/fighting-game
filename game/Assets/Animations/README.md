# Shared animation libraries

- `ual1_standard.glb`, `ual2_standard.glb`: Quaternius' *Universal Animation Library* 1 and 2, Standard tier,
  in-place versions (the `_RM` files have root motion). **CC0 1.0** (public domain): commercial use allowed, no
  attribution needed. Source: quaternius.com; clip previews: quaternius.com/animviewer.html.
- **Retargeting:** each `.glb`'s import maps its skeleton to Godot's `SkeletonProfileHumanoid` through a bone map
  (`ual_bone_map.tres` here; a model's own map next to it, e.g. `vroid_test_bone_map.tres`), with silhouette fixing
  on. Any model imported the same way can play these clips by name from its art file (ADR-0028 `file` + `clip`).
- Imported at 60 fps; Godot drops a clip's `_Loop` suffix and makes it loop.
- Contact frames in testman's art file come from measuring each clip in Blender at 60 fps (peak hand/foot reach
  forward of the hips, hip height for jumps, the lower foot's speed for walk/run `speed`).
