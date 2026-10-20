"""AI-generated prop (Tripo .fbx/.glb, no rig) -> a light, centred .glb + textures for Godot effects and props.

Usage (Blender 4.2+):
    blender -b --python tools/Blender/prop_to_glb.py -- <model.fbx|.glb> <out folder> <id> [faces]

Writes into the folder:
- <id>.glb: one mesh, centred on its bounds, scaled so its largest side is 1 m (art nodes scale it), split vertices
  merged and decimated to about [faces] (default FACES); no images, so Godot takes the material its import names.
- <id>_basecolor.png and <id>_normal.png: the model's textures at TEXTURE_SIZE, when it has them.
"""
import os
import sys

import bmesh
import bpy
import mathutils

FACES = 12000
TEXTURE_SIZE = 1024
MERGE_DISTANCE = 0.0001
TEXTURES = {"basecolor": ("base color", "basecolor", "albedo"), "normal": ("normal",)}


def import_model(src):
    bpy.ops.wm.read_homefile(use_empty=True)
    if src.lower().endswith(".fbx"):
        bpy.ops.import_scene.fbx(filepath=src)
    else:
        bpy.ops.import_scene.gltf(filepath=src)
    meshes = [ob for ob in bpy.data.objects if ob.type == "MESH"]
    if not meshes:
        sys.exit(f"{src}: no mesh")
    for ob in [ob for ob in bpy.data.objects if ob.type != "MESH"]:
        bpy.data.objects.remove(ob)
    with bpy.context.temp_override(selected_editable_objects=meshes, selected_objects=meshes, active_object=meshes[0],
                                   object=meshes[0]):
        bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        if len(meshes) > 1:
            bpy.ops.object.join()
    return meshes[0]


def centre_and_size(ob):
    points = [v.co for v in ob.data.vertices]
    low = mathutils.Vector([min(p[i] for p in points) for i in range(3)])
    high = mathutils.Vector([max(p[i] for p in points) for i in range(3)])
    scale = 1.0 / max(high - low)
    ob.data.transform(mathutils.Matrix.Scale(scale, 4) @ mathutils.Matrix.Translation(-(low + high) / 2))


def clean(ob, faces):
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=MERGE_DISTANCE)
    bm.to_mesh(ob.data)
    bm.free()
    if len(ob.data.polygons) > faces:
        decimate = ob.modifiers.new("decimate", "DECIMATE")
        decimate.ratio = faces / len(ob.data.polygons)
        with bpy.context.temp_override(object=ob, active_object=ob, selected_objects=[ob]):
            bpy.ops.object.modifier_apply(modifier=decimate.name)
    for polygon in ob.data.polygons:
        polygon.use_smooth = True


def textures(ob, src):
    images = {}
    for material in ob.data.materials:
        for node in material.node_tree.nodes if material and material.node_tree else []:
            if node.bl_idname == "ShaderNodeTexImage" and node.image:
                images[node.image.name.lower()] = node.image
    folder = os.path.splitext(src)[0] + ".fbm"
    for file in os.listdir(folder) if os.path.isdir(folder) else []:
        images.setdefault(file.lower(), None)
    found = {}
    for kind, words in TEXTURES.items():
        name = next((n for n in images if any(w.replace(" ", "") in n.replace("_", "").replace(" ", "") for w in words)), None)
        if name is None:
            continue
        image = images[name] or bpy.data.images.load(os.path.join(folder, next(f for f in os.listdir(folder) if f.lower() == name)))
        found[kind] = image
    return found


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    src, out_dir, prop_id = args[:3]
    faces = int(args[3]) if len(args) > 3 else FACES
    out_dir = os.path.abspath(out_dir)
    os.makedirs(out_dir, exist_ok=True)

    ob = import_model(src)
    centre_and_size(ob)
    clean(ob, faces)
    for kind, image in textures(ob, src).items():
        if image.size[0] > 0 and image.size[0] != TEXTURE_SIZE:
            image.scale(TEXTURE_SIZE, TEXTURE_SIZE)
        image.filepath_raw = os.path.join(out_dir, f"{prop_id}_{kind}.png")
        image.file_format = "PNG"
        image.save()
        print(f"TEXTURE {kind} -> {prop_id}_{kind}.png")
    for material in ob.data.materials:
        if material:
            material.name = prop_id
    ob.name = prop_id
    print(f"PROP {prop_id}: {len(ob.data.vertices)} vertices, {len(ob.data.polygons)} faces")
    bpy.ops.export_scene.gltf(
        filepath=os.path.join(out_dir, f"{prop_id}.glb"), export_format="GLB", export_yup=True,
        export_image_format="NONE", export_animations=False, export_apply=True, export_extras=False)


main()
