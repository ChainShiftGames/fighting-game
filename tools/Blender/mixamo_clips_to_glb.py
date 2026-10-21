"""Mixamo animation FBXs (downloaded "Without Skin") -> one in-place animation library .glb for Godot.

Usage (Blender 4.2+):
    blender -b --python tools/Blender/mixamo_clips_to_glb.py -- <out.glb> <clip>=<animation.fbx> ...
e.g. ... -- game/Assets/Characters/kachujin/kachujin_moves.glb idle="Bouncing Fight Idle.fbx" hitstun="Head Hit.fbx"

Each clip is named as given, so the art file names it by move. The fighting game moves the fighter, so the hips keep
only their height: travel across the floor (walks, knockback, throws) is removed. Mixamo FBXs are in centimetres
(armature scale 0.01); the .glb is in metres. Import it with the character's Mixamo bone map (tripo_to_glb.py writes
one) so the clips retarget like the other libraries in game/Assets/Animations.
"""
import os
import sys

import bpy
from mathutils import Vector

HIPS = "mixamorig:Hips"
FPS = 60


def fcurves(action):
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                yield from bag.fcurves


def import_action(path):
    before_actions, before_objects = set(bpy.data.actions), set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, use_image_search=False)
    new = [a for a in bpy.data.actions if a not in before_actions]
    if len(new) != 1:
        sys.exit(f"{path}: expected one animation, found {len(new)}")
    return new[0], [o for o in bpy.data.objects if o not in before_objects]


def keep_height_only(action, armature):
    # The hips' location is in the bone's rest frame; only its world-vertical part stays.
    to_world = (armature.matrix_world.to_3x3().normalized() @ armature.data.bones[HIPS].matrix_local.to_3x3())
    to_local = to_world.inverted()
    path = f'pose.bones["{HIPS}"].location'
    curves = sorted((c for c in fcurves(action) if c.data_path == path), key=lambda c: c.array_index)
    if len(curves) != 3:
        return
    frames = sorted({round(k.co[0]) for c in curves for k in c.keyframe_points})
    values = []
    for frame in frames:
        world = to_world @ Vector([c.evaluate(frame) for c in curves])
        values.append(to_local @ Vector((0.0, 0.0, world.z)))
    for index, curve in enumerate(curves):
        curve.keyframe_points.clear()
        curve.keyframe_points.add(len(frames))
        for point, frame, value in zip(curve.keyframe_points, frames, values):
            point.co = (frame, value[index])
            point.interpolation = "LINEAR"
        curve.update()


def scale_locations(action, factor):
    for curve in fcurves(action):
        if curve.data_path.endswith(".location"):
            for point in curve.keyframe_points:
                point.co[1] *= factor
                point.handle_left[1] *= factor
                point.handle_right[1] *= factor


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    out, clips = args[0], [arg.split("=", 1) for arg in args[1:]]
    bpy.ops.wm.read_homefile(use_empty=True)
    bpy.context.scene.render.fps = FPS

    armature, actions = None, []
    for name, path in clips:
        action, objects = import_action(path)
        if armature is None:
            armature = next(o for o in objects if o.type == "ARMATURE")
            objects.remove(armature)
        for ob in objects:
            bpy.data.objects.remove(ob)
        action.name = name
        actions.append(action)

    # The .import's bone map setting is keyed on the node path "Armature/Skeleton3D".
    armature.name = "Armature"
    scale = armature.scale.x
    for action in actions:
        keep_height_only(action, armature)
        scale_locations(action, scale)
    with bpy.context.temp_override(selected_editable_objects=[armature], selected_objects=[armature],
                                   active_object=armature, object=armature):
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    armature.animation_data.action = None
    for action in actions:
        track = armature.animation_data.nla_tracks.new()
        track.name = action.name
        strip = track.strips.new(action.name, int(action.frame_range[0]), action)
        if armature.animation_data.action_suitable_slots:
            strip.action_slot = armature.animation_data.action_suitable_slots[0]
        print(f"CLIP {action.name}: frames {action.frame_range[:]}")

    bpy.ops.export_scene.gltf(
        filepath=os.path.abspath(out), export_format="GLB", export_yup=True, export_image_format="NONE",
        export_skins=True, export_morph=False, export_animations=True, export_animation_mode="NLA_TRACKS",
        export_force_sampling=True, export_apply=False, export_extras=False)


main()
