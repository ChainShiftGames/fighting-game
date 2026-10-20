"""AI-generated character (Tripo mesh, Mixamo rig) -> cleaned .glb + toon material + bone map for Godot (the Tripo
trial of the character pipeline).

Usage (Blender 4.2+): rig the unrigged Tripo model in Mixamo (T-pose, "With Skin"), then
    blender -b --python tools/Blender/tripo_to_glb.py -- <model.fbx|.glb> game/Assets/Characters/<id> <id>
Tripo's own rigged export (Oct 2026) is unusable: its bones carry no transforms and its bind matrices don't match
the mesh, so the mesh shears apart as soon as a bone moves.

Writes into the character folder:
- <id>.glb: the skinned mesh in its bind pose, junk objects removed, split vertices merged, and normals smoothed
  for the toon shader (copied from a smoothed copy of the mesh, as Xrd-style models edit theirs), so AI-made bumps
  don't show as noisy shadow edges. No images; Godot gets the material below.
- <id>_body_lit.png: the base colour texture, scaled to TEXTURE_SIZE. A model in many parts, each with its own
  texture (Tripo's "convert" export), is joined into one mesh and its textures packed unscaled into one atlas.
- <id>_body.tres: a toon ShaderMaterial with an outline pass. The .glb's import maps the material "body" to it.
- <id>_bone_map.tres: Mixamo bone names -> Godot's humanoid profile, for the .glb's retarget import setting.
"""
import os
import sys

import bmesh
import bpy
import numpy

TOON = "res://Assets/Shaders/toon.gdshader"
OUTLINE = "res://Assets/Shaders/toon_outline.gdshader"
TEXTURE_SIZE = 2048
MERGE_DISTANCE = 0.0001
# Normal smoothing: how far the copy is smoothed (factor x repeats); more irons out more bumps and rounds more edges.
SMOOTH_FACTOR = 0.5
SMOOTH_REPEAT = 8
MATERIAL = "body"
# Godot turns the ':' in "mixamorig:Hips" into '_'.
MIXAMO_PREFIX = "mixamorig_"
MIXAMO_BONES = {"Hips": "Hips", "Spine": "Spine", "Chest": "Spine1", "UpperChest": "Spine2", "Neck": "Neck",
                "Head": "Head"}
for _side in ("Left", "Right"):
    MIXAMO_BONES.update({f"{_side}Shoulder": f"{_side}Shoulder", f"{_side}UpperArm": f"{_side}Arm",
                         f"{_side}LowerArm": f"{_side}ForeArm", f"{_side}Hand": f"{_side}Hand",
                         f"{_side}UpperLeg": f"{_side}UpLeg", f"{_side}LowerLeg": f"{_side}Leg",
                         f"{_side}Foot": f"{_side}Foot", f"{_side}Toes": f"{_side}ToeBase"})
    for _finger, _mixamo in (("Thumb", "Thumb"), ("Index", "Index"), ("Middle", "Middle"), ("Ring", "Ring"),
                             ("Little", "Pinky")):
        _segments = ("Metacarpal", "Proximal", "Distal") if _finger == "Thumb" else ("Proximal", "Intermediate", "Distal")
        for _number, _segment in enumerate(_segments, 1):
            MIXAMO_BONES[f"{_side}{_finger}{_segment}"] = f"{_side}Hand{_mixamo}{_number}"


def res_path(path):
    # Godot path of a file inside the project that holds project.godot.
    root = os.path.abspath(path)
    while not os.path.exists(os.path.join(root, "project.godot")):
        parent = os.path.dirname(root)
        if parent == root:
            sys.exit(f"{path} is not inside a Godot project")
        root = parent
    return "res://" + os.path.relpath(os.path.abspath(path), root).replace(os.sep, "/")


def toon_material(texture_path):
    texture = 'ExtResource("texture")'
    return "\n".join([
        '[gd_resource type="ShaderMaterial" format=3]', "",
        f'[ext_resource type="Shader" path="{TOON}" id="toon"]',
        f'[ext_resource type="Texture2D" path="{res_path(texture_path)}" id="texture"]',
        f'[ext_resource type="Shader" path="{OUTLINE}" id="outline_shader"]', "",
        '[sub_resource type="ShaderMaterial" id="outline"]', 'shader = ExtResource("outline_shader")',
        f"shader_parameter/lit_texture = {texture}",
        "shader_parameter/outline_color = Color(0.36, 0.38, 0.46, 1.0)", "",
        "[resource]", 'shader = ExtResource("toon")', 'next_pass = SubResource("outline")',
        f"shader_parameter/lit_texture = {texture}",
        "shader_parameter/shade_color = Color(0.86, 0.84, 0.92, 1.0)",
        "shader_parameter/shading_shift = -0.05",
        "shader_parameter/shading_toony = 0.95", ""])


def bone_map(armature):
    names = {bone.name.replace(":", "_") for bone in armature.data.bones}
    lines = ['[gd_resource type="BoneMap" format=3]', "", '[sub_resource type="SkeletonProfileHumanoid" id="humanoid"]', "",
             "[resource]", 'profile = SubResource("humanoid")']
    for profile, mixamo in MIXAMO_BONES.items():
        if MIXAMO_PREFIX + mixamo in names:
            lines.append(f'bone_map/{profile} = &"{MIXAMO_PREFIX}{mixamo}"')
        else:
            print(f"BONE MAP: no {MIXAMO_PREFIX}{mixamo} for {profile}")
    return "\n".join(lines + [""])


def merge_split_vertices(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=MERGE_DISTANCE)
    bm.to_mesh(mesh)
    bm.free()
    for polygon in mesh.polygons:
        polygon.use_smooth = True


def smooth_normals(body):
    # A smoothed copy gives the normals; the body keeps its shape and takes them as custom normals.
    proxy = body.copy()
    proxy.data = body.data.copy()
    proxy.modifiers.clear()
    bpy.context.scene.collection.objects.link(proxy)
    smooth = proxy.modifiers.new("smooth", "SMOOTH")
    smooth.factor, smooth.iterations = SMOOTH_FACTOR, SMOOTH_REPEAT
    transfer = body.modifiers.new("normals", "DATA_TRANSFER")
    transfer.object = proxy
    transfer.use_loop_data = True
    transfer.data_types_loops = {"CUSTOM_NORMAL"}
    transfer.loop_mapping = "POLYINTERP_NEAREST"
    with bpy.context.temp_override(object=body, active_object=body, selected_objects=[body]):
        bpy.ops.object.modifier_move_to_index(modifier=transfer.name, index=0)
        bpy.ops.object.modifier_apply(modifier=transfer.name)
    bpy.data.objects.remove(proxy)


def base_colour(material, src):
    image = next((n.image for n in material.node_tree.nodes if n.bl_idname == "ShaderNodeTexImage" and n.image), None)
    if image is not None and image.size[0] > 0:
        return image
    # An FBX keeps its textures beside it, in a "<name>.fbm" folder, which the importer doesn't always find.
    folder = os.path.splitext(src)[0] + ".fbm"
    found = sorted(f for f in os.listdir(folder) if "basecolor" in f.lower()) if os.path.isdir(folder) else []
    # A model in parts names each texture after its part's material ("Material_tripo_part_3" -> "..._part_3_basecolor").
    part = material.name.removeprefix("Material_").lower() + "_basecolor"
    found = [f for f in found if part in f.lower()] or found
    if not found:
        sys.exit(f"{material.name}: no base colour texture in the file or in {folder}")
    return bpy.data.images.load(os.path.join(folder, found[0]))


def pack_squares(sizes):
    # Power-of-two cells, largest first, each split off the smallest free square that fits; the atlas grows until
    # all fit. Returns the atlas width and height and each cell's (x, y) in pixels.
    width = max(sizes)
    while True:
        for height in (width, width * 2):
            free = [(0, y, width) for y in range(0, height, width)]
            spots = {}
            for index in sorted(range(len(sizes)), key=lambda i: -sizes[i]):
                fits = [square for square in free if square[2] >= sizes[index]]
                if not fits:
                    break
                x, y, size = min(fits, key=lambda square: square[2])
                free.remove((x, y, size))
                while size > sizes[index]:
                    size //= 2
                    free += [(x + size, y, size), (x, y + size, size), (x + size, y + size, size)]
                spots[index] = (x, y)
            if len(spots) == len(sizes):
                return width, height, spots
        width *= 2


def atlas(body, images, path):
    # One texture for the joined parts: each part's texture in its own cell, its UVs moved into that cell.
    sizes = [1 << (max(image.size) - 1).bit_length() for image in images]
    width, height, spots = pack_squares(sizes)
    pixels = numpy.zeros((height, width, 4), dtype=numpy.float32)
    for index, image in enumerate(images):
        if tuple(image.size) != (sizes[index], sizes[index]):
            image.scale(sizes[index], sizes[index])
        x, y = spots[index]
        cell = numpy.array(image.pixels[:], dtype=numpy.float32).reshape(sizes[index], sizes[index], 4)
        pixels[y:y + sizes[index], x:x + sizes[index]] = cell
    uvs = body.data.uv_layers.active.data
    for polygon in body.data.polygons:
        x, y = spots[polygon.material_index]
        size = sizes[polygon.material_index]
        for loop in polygon.loop_indices:
            u, v = uvs[loop].uv
            uvs[loop].uv = ((x + u * size) / width, (y + v * size) / height)
        polygon.material_index = 0
    result = bpy.data.images.new("atlas", width, height, alpha=True)
    result.pixels.foreach_set(pixels.ravel())
    result.filepath_raw = path
    result.file_format = "PNG"
    result.save()
    while len(body.material_slots) > 1:
        body.active_material_index = len(body.material_slots) - 1
        with bpy.context.temp_override(object=body, active_object=body):
            bpy.ops.object.material_slot_remove()
    print(f"ATLAS {len(images)} textures -> {width}x{height}")


def join(bodies):
    with bpy.context.temp_override(active_object=bodies[0], object=bodies[0], selected_objects=bodies,
                                   selected_editable_objects=bodies):
        bpy.ops.object.join()
    return bodies[0]


def main():
    src, out_dir, char_id = sys.argv[sys.argv.index("--") + 1:][:3]
    out_dir = os.path.abspath(out_dir)
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.wm.read_homefile(use_empty=True)
    if src.lower().endswith(".fbx"):
        # Image search walks the folders around the file, which can take forever; base_colour finds the textures.
        bpy.ops.import_scene.fbx(filepath=src, use_image_search=False)
    else:
        bpy.ops.import_scene.gltf(filepath=src)

    armatures = [ob for ob in bpy.data.objects if ob.type == "ARMATURE"]
    # Tripo leaves stray objects (an unskinned icosphere); only meshes the armature moves are the character.
    for ob in list(bpy.data.objects):
        if ob.type == "MESH" and not any(m.type == "ARMATURE" for m in ob.modifiers):
            bpy.data.objects.remove(ob)
    bodies = [ob for ob in bpy.data.objects if ob.type == "MESH"]
    if len(armatures) != 1 or not bodies:
        sys.exit(f"expected one armature and a skinned mesh, found {len(armatures)} and {len(bodies)}")

    # The .import's bone map setting is keyed on the node path "Armature/Skeleton3D"; some exports name it otherwise.
    armatures[0].name = "Armature"

    # Mixamo-style FBX rigs are in centimetres (armature scale 0.01); Godot's retargeting wants real metres.
    with bpy.context.temp_override(selected_editable_objects=armatures + bodies, selected_objects=armatures + bodies,
                                   active_object=armatures[0], object=armatures[0]):
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    texture_path = os.path.join(out_dir, f"{char_id}_{MATERIAL}_lit.png")
    atlased = len(bodies) > 1 or len(bodies[0].material_slots) > 1
    if atlased:
        bodies = [join(bodies)]
        atlas(bodies[0], [base_colour(slot.material, src) for slot in bodies[0].material_slots], texture_path)
    for body in bodies:
        merge_split_vertices(body.data)
        smooth_normals(body)
        for slot in body.material_slots:
            material = slot.material
            if not atlased:
                image = base_colour(material, src)
                image.scale(TEXTURE_SIZE, TEXTURE_SIZE)
                image.filepath_raw = texture_path
                image.file_format = "PNG"
                image.save()
            material.name = MATERIAL
        print(f"BODY {body.name}: {len(body.data.vertices)} vertices, {len(body.data.polygons)} faces")

    with open(os.path.join(out_dir, f"{char_id}_bone_map.tres"), "w", newline="\n") as f:
        f.write(bone_map(armatures[0]))

    tres_path = os.path.join(out_dir, f"{char_id}_{MATERIAL}.tres")
    with open(tres_path, "w", newline="\n") as f:
        f.write(toon_material(os.path.join(out_dir, f"{char_id}_{MATERIAL}_lit.png")))
    print(f"MATERIAL {MATERIAL} -> {res_path(tres_path)}")

    bpy.ops.export_scene.gltf(
        filepath=os.path.join(out_dir, f"{char_id}.glb"), export_format="GLB", export_yup=True,
        export_image_format="NONE", export_skins=True, export_morph=False, export_animations=False,
        export_apply=False, export_extras=False, export_normals=True)


main()
