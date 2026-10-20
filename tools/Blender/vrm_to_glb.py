"""VRoid/VRM model -> .glb + toon materials for Godot (Spike B pipeline, ADR-0027/0028).

Usage (needs Blender 4.2+ with the "VRM format" extension installed):
    blender -b --python tools/Blender/vrm_to_glb.py -- <model.vrm> game/Assets/Characters/<id> <id>

Writes into the character folder:
- <id>.glb: skinned mesh, face shape keys, rest pose; no images (Godot gets the materials below), no MToon outline
  hulls or spring-bone colliders.
- <id>_<part>_lit.png: one texture per material. VRoid's shade texture is the same image, tinted by the shade colour.
- <id>_<part>.tres: a toon ShaderMaterial per material from its MToon values, with an outline pass where VRoid has one.
The .glb's import settings then map each glTF material to its .tres by name (see the printed list).
"""
import bpy, sys, os, re

TOON = "res://Assets/Shaders/toon.gdshader"
TOON_DOUBLE_SIDED = "res://Assets/Shaders/toon_double_sided.gdshader"
OUTLINE = "res://Assets/Shaders/toon_outline.gdshader"
FACE_SKIN = re.compile(r"^face_\d+_skin$")
FACE_NORMAL_FLATTEN = 0.8


def clean_name(name):
    # "N00_010_01_Onepiece_00_CLOTH_01 (Instance)" -> "onepiece_00_cloth_01"
    name = re.sub(r"^N\d+_\d+(_\d+)?_", "", name.replace(" (Instance)", ""))
    return re.sub(r"[^a-z0-9]+", "_", name.lower()).strip("_")


def part_names(materials):
    # Two VRoid garment slots can carry the same part name ("N00_007_01_Tops_01_..." and "N00_002_03_Tops_01_...");
    # those keep their slot number so neither overwrites the other's files.
    names = {m: clean_name(m.name) for m in materials}
    counts = {}
    for name in names.values():
        counts[name] = counts.get(name, 0) + 1
    for material, name in names.items():
        slot = re.match(r"^N\d+_(\d+)_(\d+)_", material.name)
        if counts[name] > 1 and slot:
            names[material] = f"{name}_{slot.group(1)}_{slot.group(2)}"
    return names


def res_path(path):
    # Godot path of a file inside the project that holds project.godot.
    root = os.path.abspath(path)
    while not os.path.exists(os.path.join(root, "project.godot")):
        parent = os.path.dirname(root)
        if parent == root:
            sys.exit(f"{path} is not inside a Godot project")
        root = parent
    return "res://" + os.path.relpath(os.path.abspath(path), root).replace(os.sep, "/")


def srgb(channel):
    # MToon factors are linear; Godot source_color values in a .tres are sRGB.
    return channel * 12.92 if channel <= 0.0031308 else 1.055 * channel ** (1 / 2.4) - 0.055


def color(factor):
    rgb = ", ".join(f"{srgb(c):.4f}" for c in factor[:3])
    return f"Color({rgb}, {factor[3] if len(factor) > 3 else 1.0:.4f})"


def save_png(image, path):
    image.filepath_raw = path
    image.file_format = "PNG"
    image.save()


def toon_material(material, texture_path):
    mt = material.vrm_addon_extension.mtoon1
    ext = mt.extensions.vrmc_materials_mtoon
    texture = f'ExtResource("texture")'
    cutoff = mt.alpha_cutoff if mt.alpha_mode != "OPAQUE" else 0.0
    lines = ['[gd_resource type="ShaderMaterial" format=3]', "",
             f'[ext_resource type="Shader" path="{TOON_DOUBLE_SIDED if mt.double_sided else TOON}" id="toon"]',
             f'[ext_resource type="Texture2D" path="{res_path(texture_path)}" id="texture"]']
    outline = ext.outline_width_mode != "none"
    if outline:
        lines += [f'[ext_resource type="Shader" path="{OUTLINE}" id="outline_shader"]', "",
                  '[sub_resource type="ShaderMaterial" id="outline"]', 'shader = ExtResource("outline_shader")',
                  f"shader_parameter/lit_texture = {texture}",
                  f"shader_parameter/outline_color = {color(ext.outline_color_factor)}",
                  f"shader_parameter/alpha_cutoff = {cutoff:.4f}"]
    lines += ["", "[resource]", 'shader = ExtResource("toon")'] + (['next_pass = SubResource("outline")'] if outline else []) + [
        f"shader_parameter/lit_texture = {texture}",
        f"shader_parameter/lit_color = {color(mt.pbr_metallic_roughness.base_color_factor)}",
        f"shader_parameter/shade_color = {color(ext.shade_color_factor)}",
        f"shader_parameter/shading_shift = {ext.shading_shift_factor:.4f}",
        f"shader_parameter/shading_toony = {ext.shading_toony_factor:.4f}",
        f"shader_parameter/alpha_cutoff = {cutoff:.4f}"]
    if FACE_SKIN.search(clean_name(material.name)):
        # Flat face shading, as Xrd's edited face normals; the toon shader bends normals towards the camera.
        lines.append(f"shader_parameter/normal_flatten = {FACE_NORMAL_FLATTEN:.4f}")
    return "\n".join(lines + [""])


def main():
    src, out_dir, char_id = sys.argv[sys.argv.index("--") + 1:][:3]
    # Blender resolves image paths against the .blend file, so a relative folder would save no textures.
    out_dir = os.path.abspath(out_dir)
    os.makedirs(out_dir, exist_ok=True)
    bpy.ops.wm.read_homefile(use_empty=True)
    bpy.ops.preferences.addon_enable(module="bl_ext.blender_org.vrm")
    bpy.ops.import_scene.vrm(filepath=src)

    # Godot draws its own outline; the MToon hulls are geometry-nodes modifiers with extra materials.
    for ob in list(bpy.data.objects):
        if ob.type == "EMPTY":
            bpy.data.objects.remove(ob)
        elif ob.type == "MESH":
            for mod in [m for m in ob.modifiers if m.type == "NODES"]:
                ob.modifiers.remove(mod)
    used = {m for ob in bpy.data.objects if ob.type == "MESH" for m in ob.data.materials if m}

    names = part_names(used)
    for material in sorted(used, key=lambda m: m.name):
        name = names[material]
        texture_path = os.path.join(out_dir, f"{char_id}_{name}_lit.png")
        save_png(material.vrm_addon_extension.mtoon1.pbr_metallic_roughness.base_color_texture.index.source, texture_path)
        tres_path = os.path.join(out_dir, f"{char_id}_{name}.tres")
        with open(tres_path, "w", newline="\n") as f:
            f.write(toon_material(material, texture_path))
        # The glTF keeps only the name; Godot swaps in the .tres.
        material.name = name
        material.node_tree.nodes.clear()
        output = material.node_tree.nodes.new("ShaderNodeOutputMaterial")
        bsdf = material.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
        material.node_tree.links.new(bsdf.outputs[0], output.inputs[0])
        print(f"MATERIAL {name} -> {res_path(tres_path)}")

    bpy.ops.export_scene.gltf(
        filepath=os.path.join(out_dir, f"{char_id}.glb"), export_format="GLB", export_yup=True,
        export_image_format="NONE", export_skins=True, export_morph=True, export_animations=False,
        export_apply=False, export_extras=False)


main()
